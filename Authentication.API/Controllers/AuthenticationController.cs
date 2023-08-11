using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Account.API.Data;
using Authentication.API.Data;
using Authentication.API.Model;
using Authentication.API.Security.Hashing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace Authentication.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly AuthenticationDbContext _authenticationDbContext;

        public AuthenticationController(AuthenticationDbContext dbContext)
        {
            _authenticationDbContext = dbContext;
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(UserRegisterModel userRegisterModel)
        {
            try
            {
                // Kullanıcının şifresini hash'leyip veritabanına kaydediyoruz
                HashingHelper.CreatePasswordHash(userRegisterModel.Password, out var passwordHash, out var passwordSalt);

                var userPassword = new UserPassword
                {
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt
                };

                _authenticationDbContext.Add(userPassword);
                await _authenticationDbContext.SaveChangesAsync();

                // JWT üretimi
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes("3KBsVR697nrsqxfvvjlZDw=="); // Gizli anahtar
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                        new Claim(ClaimTypes.Name, userRegisterModel.UserName)
                        // Diğer talepleri buraya ekleyebilirsiniz
                    }),
                    Expires = DateTime.UtcNow.AddHours(1), // Token süresi
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                // Kullanıcı kayıt modelini JSON formatına dönüştürüyoruz
                string jsonContent = JsonConvert.SerializeObject(userRegisterModel);

                // JSON içeriğini HTTP isteği içeriği olarak ayarlıyoruz
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Gateway endpoint adresini belirliyoruz
                var endpoint = "/Authentication/CreateUser";

                // Gateway ana adresi ve endpoint adresini belirliyoruz
                var gatewayBaseUrl = "https://localhost:7244";
                var apiUrl = $"{gatewayBaseUrl}{endpoint}";

                using (var client = new HttpClient())
                {
                    // Gateway üzerinden kullanıcı kayıt isteği atıyoruz
                    var response = await client.PostAsync(apiUrl, httpContent);
                    response.EnsureSuccessStatusCode(); // Hata durumunu kontrol ediyoruz
                }

                return Ok(new { Token = tokenString }); // Başarılı ise token ile 200 OK cevabı dönüyoruz
            }
            catch (HttpRequestException ex)
            {
                // Gateway üzerinde hata oluşursa yakalıyoruz
                return StatusCode(500, "Gateway error: " + ex.Message); // 500 Internal Server Error dönüyoruz
            }
            catch (Exception ex)
            {
                // Diğer hataları yakalıyoruz
                return StatusCode(500, "Internal error: " + ex.Message); // 500 Internal Server Error dönüyoruz
            }
        }

        
    }
}
