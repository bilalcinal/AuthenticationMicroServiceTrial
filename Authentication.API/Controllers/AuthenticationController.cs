using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Account.API.Model;
using Authentication.API.Data;
using Authentication.API.Model;
using Authentication.API.Security.Hashing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Ocelot.Responses;
using System.Reflection.Metadata.Ecma335;
using Ocelot.Errors;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json.Linq;
using Ocelot.Middleware;
using System.Net;
using Account.API.Data;

namespace Authentication.API.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly AuthenticationDbContext _authenticationDbContext;

        public AuthenticationController(AuthenticationDbContext dbContext)
        {
            _authenticationDbContext = dbContext;
        }

        [HttpPost] 
        public async Task<AuthRegisterResponseModel> Register(AuthRegisterRequestModel AuthRegisterRequestModel) 
        {
            try
            {
                // Kullanıcı kayıt modelini JSON formatına dönüştürüyoruz
                string jsonContent = JsonConvert.SerializeObject(AuthRegisterRequestModel);

                // JSON içeriğini HTTP isteği içeriği olarak ayarlıyoruz
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Gateway endpoint adresini belirliyoruz
                var endpoint = "/Account/CreateAccount";

                // Gateway ana adresi ve endpoint adresini belirliyoruz
                var gatewayBaseUrl = "https://localhost:7244";
                var apiUrl = $"{gatewayBaseUrl}{endpoint}";

                var accountData = new AccountGetAccountModel();
                using (var client = new HttpClient())
                {   // Gateway üzerinden kullanıcı kayıt isteği atıyoruz
                    var restResponse = await client.PostAsync(apiUrl, httpContent);
                    var responseString = await restResponse.Content.ReadAsStringAsync();
                    accountData = JsonConvert.DeserializeObject<AccountGetAccountModel>(responseString);
                    restResponse.EnsureSuccessStatusCode();
                }
                if(AuthRegisterRequestModel.Password != AuthRegisterRequestModel.PasswordAgain)
                {
                    var errorResponse = new AuthRegisterResponseModel
                    {
                        Error = "Şifreler eşleşmiyor. Lütfen tekrar deneyiniz!"
                    };
                    return errorResponse;
                }
                
                // Kullanıcının şifresini hash'leyip veritabanına kaydediyoruz
                HashingHelper.CreatePasswordHash(AuthRegisterRequestModel.Password, out var passwordHash, out var passwordSalt);
                var AccountPassword = new AuthPassword
                {
                    AccountId = accountData.Id,
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt
                };

                _authenticationDbContext.Add(AccountPassword);
                await _authenticationDbContext.SaveChangesAsync();


                //JWT oluşturuluyor
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes("3KBsVR697nrsqxfvvjlZDw==");
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                        new Claim(ClaimTypes.Email, AuthRegisterRequestModel.Email)

                    }),
                    Expires = DateTime.UtcNow.AddDays(7), // Token süresi
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);
                
                var response = new AuthRegisterResponseModel
                {
                    Token = tokenString
                };

                return response;
            }
            catch (HttpRequestException ex)
            {
                // model oluştur reponse dön
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Login(AuthLoginModel model)
        {
            try
            {
                // Gateway endpoint adresini belirliyoruz
                var endpoint = "/Account/GetAccount?email=" + model.Email;

                // Gateway ana adresi ve endpoint adresini belirliyoruz
                var gatewayBaseUrl = "https://localhost:7244";
                var apiUrl = $"{gatewayBaseUrl}{endpoint}";

                using (var client = new HttpClient())
                {
                    var restResponse = await client.GetAsync(apiUrl);
                    if (restResponse.IsSuccessStatusCode)
                    {
                        // deaserialize object from response
                        var responseString = await restResponse.Content.ReadAsStringAsync();
                        var accountData = JsonConvert.DeserializeObject<AccountGetAccountModel>(responseString);

                        var accountPasswordData = await _authenticationDbContext.AuthPasswords
                                                            .Where(p => p.AccountId == accountData.Id)
                                                            .FirstOrDefaultAsync();
                        bool isPasswordValid = HashingHelper.VerifyPasswordHash(model.Password, accountPasswordData.PasswordHash, accountPasswordData.PasswordSalt);
                        if (!isPasswordValid)
                        {
                            return NotFound(); // Şifre doğrulanamazsa NotFound döndürülür
                        }
                        // Kullanıcının hesabı doğrulandıysa JWT Token oluşturulur
                        var tokenHandler = new JwtSecurityTokenHandler();
                        var key = Encoding.ASCII.GetBytes("3KBsVR697nrsqxfvvjlZDw==");
                        var tokenDescriptor = new SecurityTokenDescriptor
                        {
                            Subject = new ClaimsIdentity(new Claim[]
                            {
                                new Claim(ClaimTypes.Email, model.Email)
                            }),
                            Expires = DateTime.UtcNow.AddMinutes(10), // Token süresi
                            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                        };
                        var token = tokenHandler.CreateToken(tokenDescriptor);
                        var tokenString = tokenHandler.WriteToken(token);

                        return Ok(new { Token = tokenString });
                    }
                    else
                    {
                        return NotFound(); 
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, "Gateway error: " + ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal error: " + ex.Message);
            }
        }
        public async Task<IActionResult> UpdatePassword()
        {
            try
            {
                // JWT token onaylama
                var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes("3KBsVR697nrsqxfvvjlZDw==");

                ClaimsPrincipal principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                // Token süresi kontrolü
                if (validatedToken.ValidTo < DateTime.UtcNow)
                {
                    return Unauthorized();
                }

                // Token'dan email çekiliyor
                var userEmailClaim = principal.FindFirst(ClaimTypes.Email);
                if (userEmailClaim == null)
                {
                    return Unauthorized();
                }
                var userEmail = userEmailClaim.Value;
                /* Jwt tokenden email alınacak
                 * Ocelot ile alınan emailin ıd si ile account bilgisi çekilecek
                 * AccountId ile password bulunacak
                 * daha sonra HttpClient.VerifyPasswordHash ile 
                   girilen password ile database de ki password karşılaştırılacak
                 * Eğer hepsi doğru ise girilen yeni şifre hashlenip saltlanarak veritabanına kayıt edilecek
                 * En son Jwt token oluşturarak response olarak token döndürecek
                 */

                // Jwt oluşturuyoruz
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                new Claim(ClaimTypes.Email, userEmail)
                    }),
                    Expires = DateTime.UtcNow.AddMinutes(10),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };
                var newToken = tokenHandler.CreateToken(tokenDescriptor);
                var newTokenString = tokenHandler.WriteToken(newToken);

                return Ok(new { Token = newTokenString, Message = "Kullanıcı bilgileri güncellendi" });


            }
            catch (Exception ex)
            {
                // Hata yönetimi burada yapılabilir
                return StatusCode(500, "Sunucu hatası");
            }
        }
    }
}




        
    
