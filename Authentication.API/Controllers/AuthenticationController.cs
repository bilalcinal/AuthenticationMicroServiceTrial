using Account.API.Data;
using Authentication.API.Data;
using Authentication.API.Model;
using Authentication.API.Security.Hashing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Authentication.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly AuthenticationDbContext _authenticationDbContex;


         public AuthenticationController(AuthenticationDbContext dbContext)
        {
            _authenticationDbContex = dbContext;
           
        }


        // [HttpPost("CreateUser")]
        // public async Task<IActionResult> CreateUser(UserRegisterModel userRegisterModel)
        // {

        //     var client = new HttpClient();
        //     string jsonContent = JsonConvert.SerializeObject(userRegisterModel);

        //     var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

        //     var gatewayBaseUrl = "https://localhost:7244"; // Gateway ana adresi
        //     var endpoint = "/api/User/CreateUser"; // Endpoint adresi

        //     var apiUrl = $"{gatewayBaseUrl}{endpoint}";
        //     var response = await client.PostAsync(apiUrl, httpContent);
        //     //var response = await client.PostAsync("gateway + account create user endpoint adresi", httpContent);

        //     HashingHelper.CreatePasswordHash(userRegisterModel.Password , out var passwordHash, out var passwordSalt);

        //     var UserPassword = new UserPassword
        //     {
        //      PasswordHash = passwordHash,
        //      PasswordSalt = passwordSalt
        //     };

        //   _authenticationDbContex.Add(UserPassword);
        //   await _authenticationDbContex.SaveChangesAsync();
        //   return Ok();
        // }

        [HttpPost("CreateUser")]
        public async Task<IActionResult> CreateUser(UserRegisterModel userRegisterModel)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // Kullanıcı kayıt modelini JSON formatına dönüştürüyoruz
                    string jsonContent = JsonConvert.SerializeObject(userRegisterModel);

                    // JSON içeriğini HTTP isteği içeriği olarak ayarlıyoruz
                    var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                    // Gateway endpoint adresini belirliyoruz
                    var endpoint = "/Authentication/CreateUser";

                    // Gateway ana adresi ve endpoint adresini belirliyoruz
                    var gatewayBaseUrl = "https://localhost:7244";
                    var apiUrl = $"{gatewayBaseUrl}{endpoint}";

                    // Gateway üzerinden kullanıcı kayıt isteği atıyoruz
                    var response = await client.PostAsync(apiUrl, httpContent);
                    response.EnsureSuccessStatusCode(); // Hata durumunu kontrol ediyoruz

                    // Kullanıcının şifresini hash'leyip veritabanına kaydediyoruz
                    HashingHelper.CreatePasswordHash(userRegisterModel.Password, out var passwordHash, out var passwordSalt);

                    var UserPassword = new UserPassword
                    {
                        PasswordHash = passwordHash,
                        PasswordSalt = passwordSalt
                    };

                    _authenticationDbContex.Add(UserPassword);
                    await _authenticationDbContex.SaveChangesAsync();

                    return Ok(); // Başarılı ise 200 OK cevabı dönüyoruz
                }
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



        [HttpPost("UpdateUser")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserRegisterModel userRegisterModel)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingUser = await _authenticationDbContex.UserPassword.FindAsync(id);
            if (existingUser == null)
                return NotFound();

            try
            {
                await _authenticationDbContex.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(500);
            }

            return Ok(); 
        }


    }
}