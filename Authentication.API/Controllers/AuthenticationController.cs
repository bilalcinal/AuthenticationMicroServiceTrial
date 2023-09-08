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
using Microsoft.EntityFrameworkCore;
using Notification.API.Model;
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
        #region AuthenticationConstants
        public static class AuthenticationConstants
        {
            public const string GatewayBaseUrl = "https://localhost:7244";
        }
        #endregion

        #region Register
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
                var apiUrl = $"{AuthenticationConstants.GatewayBaseUrl}{endpoint}";

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
                    Expires = DateTime.UtcNow.AddMinutes(5)
                };

                _authenticationDbContext.Add(authToken);
                await _authenticationDbContext.SaveChangesAsync();

               
                var sendMailEndpoint = "/Notification/SendEmail";
                var NotificationApiUrl = $"{AuthenticationConstants.GatewayBaseUrl}{sendMailEndpoint}";

                var emailModel = new EmailModel
                {
                    ToEmail = AuthRegisterRequestModel.Email,
                    Subject = "Hoş Geldiniz!",
                    Body = @"<!DOCTYPE html>
                                <html lang=""en"">
                                <head>
                                    <meta charset=""UTF-8"">
                                    <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
                                    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                                    <title>Complete Registration</title>
                                </head>
                                <body>
                                    <h1>Welcome to our platform!</h1>
                                    <p>Thank you for registering. To complete your registration, please click the following link:</p>
                                    <p><a href=""https://localhost:7244/Authentication/ValidateTokenCallback?validationToken=" + token + @""">Complete Registration</a></p>
                                    <p>If the link above doesn't work, you can copy and paste the following URL into your browser's address bar:</p>
                                    <p>https://localhost:7244/Authentication/ValidateTokenCallback?validationToken=" + token + @"</p>
                                    <p>We're excited to have you on board. If you have any questions, feel free to contact us.</p>
                                    <p>Best regards,</p>
                                    <p>Your Team</p>
                                </body>
                                </html>
                                "
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
        #endregion

        #region ValidateTokenCallback
        [HttpGet]
        public async Task<IActionResult> ValidateTokenCallback(string validationToken)
        {
            try
            {
                var authToken = await _authenticationDbContext.AuthAccessTokens
                                    .Where(t => t.Token == validationToken && t.Expires > DateTime.UtcNow)
                                    .FirstOrDefaultAsync();

                if (authToken != null )
                {
                    // Gateway endpoint adresini belirliyoruz
                    var endpoint = "/Account/ActivateAccount";
                    var activateAccountUrl = $"{AuthenticationConstants.GatewayBaseUrl}{endpoint}";

                    var activationRequest = new ActivateAccountModel
                    {
                        AccountId = authToken.AccountId
                    };

                    using (var httpClient = new HttpClient())
                    {
                        var activationResponse = await httpClient.PostAsJsonAsync(activateAccountUrl, activationRequest);
                        activationResponse.EnsureSuccessStatusCode();

                        return Ok(new { Message = "Token doğrulandı. Hesap aktive edildi." });
                    }
                }
                else
                {
                    return BadRequest(new { Message = "Token geçersiz veya süresi dolmuş." });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "Token doğrulama sırasında bir hata oluştu." });
            }
        }
        #endregion

        #region Login
        [HttpPost]
        public async Task<IActionResult> Login(AuthLoginModel model)
        {
            try
            {
                // Gateway endpoint adresini belirliyoruz
                var endpoint = "/Account/GetAccount?email=" + model.Email;
                var apiUrl = $"{AuthenticationConstants.GatewayBaseUrl}{endpoint}";

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
                            Expires = DateTime.UtcNow.AddMinutes(10),
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
        #endregion

        #region UpdatePassword
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
                var apiUrl = $"{AuthenticationConstants.GatewayBaseUrl}{endpoint}";

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
        #endregion

    }
}