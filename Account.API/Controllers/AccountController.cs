using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Account.API.Data;
using Account.API.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Account.API.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class AccountController : ControllerBase
    {
        /*
         * ACCOUNTCHECK
         * GETACCOUNT
         * CREATEACCOUNT
         * UPDATEACCOUNT
         */
        private readonly AccountDbContext _accountDbContext;

        public AccountController(AccountDbContext dbContext)
        {
            _accountDbContext = dbContext;
        }
        
        [HttpGet]
        public async Task<IActionResult> AccountCheck(string Email)
        {
            var user = await _accountDbContext.User.Where(p => p.Email == Email).AnyAsync();
            if (!user)
            {
                return NotFound();
            }

            return Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAccount(UserModel userModel )
        {
            var User = new User{
              FirstName = userModel.FirstName,
              LastName = userModel.LastName,
              Email = userModel.Email,
              Phone = userModel.Phone,
              CreatedDate = DateTime.UtcNow
            };
             _accountDbContext.User.Add(User);
             await _accountDbContext.SaveChangesAsync();

            return Ok();
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateAccount(int Id, UserModel userModel)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                return Unauthorized();
            }

            var user = await _accountDbContext.User.Where(x => x.Id == Id).FirstOrDefaultAsync();
            
            if (user == null)
            {
                return NotFound("Girdiğiniz bilgiler yanlış veya kayıt edilmemiş. Tekrar deneyiniz");
            }
               user.FirstName = userModel.FirstName;
                user.LastName = userModel.LastName;
                user.Email = userModel.Email;
                user.Phone = userModel.Phone;
                user.ModifiedDate = userModel.ModifiedDate;

                _accountDbContext.User.Update(user);
                await _accountDbContext.SaveChangesAsync();

                

            

            return Ok("Kullanıcı bilgileri güncellendi");
        }


        [HttpGet]
        public async Task<IActionResult> GetAccount(string Email)
        {
            var user = await _accountDbContext.User.Where(p => p.Email == Email).ToListAsync();

            if (user == null)
            {
                return NotFound("Böyle bir hesap bulunmamaktadır. Lütfen geçerli bir hesap giriniz!");
            }

            return Ok(user);

        }

        

        
    }
}
