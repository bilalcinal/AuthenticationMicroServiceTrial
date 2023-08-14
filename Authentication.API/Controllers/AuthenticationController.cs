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
        public async Task<AuthRegisterResponseModel> Register(AuthRegisterRequestModel AccountRegisterModel) 
        {
            try
            {
                // Kullanıcı kayıt modelini JSON formatına dönüştürüyoruz
                string jsonContent = JsonConvert.SerializeObject(AccountRegisterModel);

                // JSON içeriğini HTTP isteği içeriği olarak ayarlıyoruz
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Gateway endpoint adresini belirliyoruz
                var endpoint = "/Account/CreateAccount";

                // Gateway ana adresi ve endpoint adresini belirliyoruz
                var gatewayBaseUrl = "https://localhost:7244";
                var apiUrl = $"{gatewayBaseUrl}{endpoint}";


                var accountData = new AccountGetAccountModel();
                using (var client = new HttpClient())
                {
                    
                    // Gateway üzerinden kullanıcı kayıt isteği atıyoruz
                    var restResponse = await client.PostAsync(apiUrl, httpContent);
                    var responseString = await restResponse.Content.ReadAsStringAsync();
                    accountData = JsonConvert.DeserializeObject<AccountGetAccountModel>(responseString);
                    if(AccountRegisterModel.Password != AccountRegisterModel.PasswordAgain)
                    {
                        var errorResponse = new AuthRegisterResponseModel
                        {
                            Error = "Şifreler eşleşmiyor. Lütfen tekrar deneyiniz!"
                        };
                        return errorResponse;
                    }
                    
                    restResponse.EnsureSuccessStatusCode();
                }
                
                // Kullanıcının şifresini hash'leyip veritabanına kaydediyoruz
                HashingHelper.CreatePasswordHash(AccountRegisterModel.Password, out var passwordHash, out var passwordSalt);

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
                        new Claim(ClaimTypes.Email, AccountRegisterModel.Email)

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
                            Expires = DateTime.UtcNow.AddDays(7), // Token süresi
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
    }
}



        
    
