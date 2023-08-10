using Authentication.API.Data;
using Authentication.API.Model;
using Microsoft.AspNetCore.Mvc;

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

        
        [HttpPost("CreateUser")]
        public async Task<IActionResult> CreateUser(UserRegisterModel userRegisterModel)
        {
            var UserRegister = new UserRegister
            {
                UserName = userRegisterModel.UserName,
                Email = userRegisterModel.Email,
                CreatedDate = DateTime.UtcNow
            };
          
          _authenticationDbContex.Add(UserRegister);
          await _authenticationDbContex.SaveChangesAsync();
          return Ok();
        }


    }
}