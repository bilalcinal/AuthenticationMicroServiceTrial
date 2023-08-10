using System;
using System.Linq;
using System.Threading.Tasks;
using Account.API.Data;
using Account.API.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Account.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly AccountDbContext _accountDbContext;

        public UserController(AccountDbContext dbContext)
        {
            _accountDbContext = dbContext;
        }

        [HttpGet("GetUserById")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _accountDbContext.User.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        [HttpPost("CreateUser")]
        public async Task<IActionResult> CreateUser(UserModel userModel )
        {
            var User = new User{
              UserName = userModel.UserName,
              Email = userModel.Email,
              Phone = userModel.Phone,
              CreatedDate = DateTime.UtcNow
            };
           
            _accountDbContext.User.Add(User);
             await _accountDbContext.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("DeleteUser")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _accountDbContext.User.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _accountDbContext.User.Remove(user);
            await _accountDbContext.SaveChangesAsync();

            return NoContent();
        }
    }
}
