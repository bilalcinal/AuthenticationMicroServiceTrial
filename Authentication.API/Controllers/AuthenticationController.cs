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
using Notification.API.Model;
using Microsoft.AspNetCore.Authentication;
using Authentication.API.Security.AccessToken;

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
                {    // Gateway üzerinden kullanıcı kayıt isteği atıyoruz
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

                // Kullanıcıya token gönderme işlemi
                var tokenGenerator = new TokenGenerator();
                var token = tokenGenerator.GenerateToken();
                var authToken = new AuthAccessToken
                {
                    AccountId = accountData.Id,
                    Token = token,
                    Expires = DateTime.UtcNow.AddMinutes(5) // Örnek olarak 5dk ayarlandı
                };

                _authenticationDbContext.Add(authToken);
                await _authenticationDbContext.SaveChangesAsync();

               
                var sendMailEndpoint = "/Notification/SendEmail";
                var NotificationApiUrl = $"{gatewayBaseUrl}{sendMailEndpoint}";

                var emailModel = new EmailModel
                {
                    ToEmail = AuthRegisterRequestModel.Email,
                    Subject = "Hoş Geldiniz!",
                    Body = $"Merhaba, hoş geldiniz! Kaydınızı tamamlamak için tokeninizi girmeniz gerekmektedir: {token}"
                };

                using (var NotificationClient = new HttpClient())
                {
                    var mailContentJson = JsonConvert.SerializeObject(emailModel);

                    var mailHttpContent = new StringContent(mailContentJson, Encoding.UTF8, "application/json");

                    var NotificationResponse = await NotificationClient.PostAsync(NotificationApiUrl, mailHttpContent);
                    NotificationResponse.EnsureSuccessStatusCode();

                    var successResponse = new AuthRegisterResponseModel
                    {
                        Success = "Kayıt başarıyla tamamlandı. Hoş geldin maili gönderildi."
                    };
                    return successResponse;
                }
            }
            catch (HttpRequestException ex)
            {
               
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        [HttpPost]
        public async Task<IActionResult> VerifyToken(RegisterTokenVerificationModel registerTokenVerificationModel)
        {
            try
            {
                var authToken = await _authenticationDbContext.AuthAccessTokens
                    .FirstOrDefaultAsync(t => t.AccountId == registerTokenVerificationModel.AccountId && t.Token == registerTokenVerificationModel.Token && t.Expires > DateTime.UtcNow);

                if (authToken != null)
                {
                    // Token doğrulandı
                    return Ok(new { Message = "Token doğrulandı." });
                }
                else
                {
                    // Token geçersiz veya süresi dolmuş
                    return BadRequest(new { Message = "Token geçersiz veya süresi dolmuş." });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "Token doğrulama sırasında bir hata oluştu." });
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
                        
                        var responseString = await restResponse.Content.ReadAsStringAsync();
                        var accountData = JsonConvert.DeserializeObject<AccountGetAccountModel>(responseString);

                        var accountPasswordData = await _authenticationDbContext.AuthPasswords
                                                            .Where(p => p.AccountId == accountData.Id)
                                                            .FirstOrDefaultAsync();
                        bool isPasswordValid = HashingHelper.VerifyPasswordHash(model.Password, accountPasswordData.PasswordHash, accountPasswordData.PasswordSalt);
                        if (!isPasswordValid)
                        {
                            return NotFound();
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
        
        [HttpPost]
        public async Task<IActionResult> UpdatePassword(AuthPasswordUpdateModel authPasswordUpdateModel)
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

                // Gateway endpoint adresi
                var endpoint = $"/Account/GetAccount?email={userEmail}";

                // Gateway ana adresi ve endpoint adresi
                var gatewayBaseUrl = "https://localhost:7244";
                var apiUrl = $"{gatewayBaseUrl}{endpoint}";

                using (var client = new HttpClient())
                {
                    var restResponse = await client.GetAsync(apiUrl);
                    if (restResponse.IsSuccessStatusCode)
                    {
                        // Gateway'den alınan veriyi deserialize ediyoruz
                        var responseString = await restResponse.Content.ReadAsStringAsync();
                        var accountData = JsonConvert.DeserializeObject<AccountGetAccountModel>(responseString);

                        // Kullanıcının eski şifre bilgisini alıyoruz
                        var accountPasswordData = await _authenticationDbContext.AuthPasswords
                                                            .Where(p => p.AccountId == accountData.Id)
                                                            .FirstOrDefaultAsync();
                        bool isPasswordValid = HashingHelper.VerifyPasswordHash(authPasswordUpdateModel.OldPassword, accountPasswordData.PasswordHash, accountPasswordData.PasswordSalt);
                        if (!isPasswordValid)
                        {
                            return BadRequest("Eski şifre doğrulanamadı");
                        }

                        // Kullanıcının yeni şifresini hash'leyip güncelliyoruz
                        HashingHelper.CreatePasswordHash(authPasswordUpdateModel.NewPassword, out var newPasswordHash, out var newPasswordSalt);
                        accountPasswordData.PasswordHash = newPasswordHash;
                        accountPasswordData.PasswordSalt = newPasswordSalt;
                        accountPasswordData.ModifiedDate = DateTime.UtcNow; 

                        _authenticationDbContext.Update(accountPasswordData);
                        await _authenticationDbContext.SaveChangesAsync();

                        // Yeni JWT token oluşturuyoruz
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

                        return Ok(new { Token = newTokenString });
                    }

                    return BadRequest("Kullanıcı bilgileri alınamadı");
                }
            }
            catch (Exception ex)
            {
               
                return StatusCode(500, "Sunucu hatası");
            }
        }


    }
}