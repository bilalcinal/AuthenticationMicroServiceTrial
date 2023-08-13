using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Authentication.API.Data;
using Authentication.API.Model;
using Authentication.API.Security.Hashing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

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
        public async Task<IActionResult> Register(UserRegisterModel userRegisterModel) 
        {
            try
            {
                // JSON içeriğini HTTP isteği
                var httpContent = new StringContent(JsonConvert.SerializeObject(userRegisterModel), Encoding.UTF8, "application/json");

                // Gateway endpoint adresi

                // Gateway ana adresi ve endpoint adresi
                var apiUrl = "https://localhost:7244" + "/Authentication/Register";

                using (var client = new HttpClient())
                {
                    // Gateway üzerinden kullanıcı kayıt isteği atıyoruz
                    var response = await client.PostAsync(apiUrl, httpContent);

                    if (!response.IsSuccessStatusCode)
                    {
                        return Ok("Kullanıcı kaydı oluşturulurken bir hata meydana geldi. Lütfen daha sonra tekrar deneyin.");
                    }
                }

                // Kullanıcının şifresini hash'leyip veritabanına kaydediyoruz
                HashingHelper.CreatePasswordHash(userRegisterModel.Password, out var passwordHash, out var passwordSalt);

                var userPassword = new UserPassword
                {
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt
                };

                _authenticationDbContext.Add(userPassword);
                await _authenticationDbContext.SaveChangesAsync();


                //JWT oluşturuluyor
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes("3KBsVR697nrsqxfvvjlZDw==");
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                        new Claim(ClaimTypes.Email, userRegisterModel.Email)

                    }),
                    Expires = DateTime.UtcNow.AddDays(7), // Token süresi
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                return Ok(new { Token = tokenString });
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
                 /*
                 *  1- Kullanıcının hesabı var mı yok mu kontrol edilir
                 *  2- JWT Token oluşturulur
                 *  3- Token response olarak döndürülür
                 */
 }
}



        
    
