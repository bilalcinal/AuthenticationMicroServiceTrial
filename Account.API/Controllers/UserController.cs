using System;
using System.Linq;
using System.Threading.Tasks;
using Account.API.Data;
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

        [HttpGet("{id}")]
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
        public async Task<IActionResult> CreateUser(User user)
        {
            _accountDbContext.User.Add(user);
            await _accountDbContext.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("{id}")]
        public async Task<IActionResult> UpdateUser(int id, User updatedUser)
        {
            if (id != updatedUser.Id)
            {
                return BadRequest();
            }

            _accountDbContext.Entry(updatedUser).State = EntityState.Modified;

            try
            {
                await _accountDbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
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

        private bool UserExists(int id)
        {
            return _accountDbContext.User.Any(u => u.Id == id);
        }
    }
}
