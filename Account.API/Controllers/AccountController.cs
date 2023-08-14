using System.Security.Claims;
using Account.API.Data;
using Account.API.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AccountEntity = Account.API.Data.Account;


namespace Account.API.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class AccountController : ControllerBase
    {
        private readonly AccountDbContext _accountDbContext;

        public AccountController(AccountDbContext dbContext)
        {
            _accountDbContext = dbContext;
        }
        
        [HttpGet]
        public async Task<IActionResult> AccountCheck(string Email)
        {
            var account = await _accountDbContext.Accounts.Where(p => p.Email == Email).AnyAsync();
            if (!account)
            {
                return NotFound();
            }

            return Ok(account);
        }

        [HttpGet]
        public async Task<AccountGetAccountModel> GetAccount(string Email)
        {
            var account = await _accountDbContext.Accounts.Where(p => p.Email == Email).Select(p => new AccountGetAccountModel()
            {
                Id = p.Id,
                FirstName = p.FirstName,
                LastName = p.LastName,
                Email = p.Email,
                Phone = p.Phone
            }).FirstOrDefaultAsync();

            if (account == null)
            {
                return null;
            }

            return account;

        }

        [HttpPost]
        public async Task<AccountGetAccountModel> CreateAccount(AccountModel accountModel )
        {
            var account = new AccountEntity
            {    
              FirstName = accountModel.FirstName,
              LastName = accountModel.LastName,
              Email = accountModel.Email,
              Phone = accountModel.Phone,
              CreatedDate = DateTime.UtcNow
            };
             _accountDbContext.Accounts.Add(account);
             await _accountDbContext.SaveChangesAsync();

            var response = new AccountGetAccountModel
            {
                Id = account.Id,
                FirstName = account.FirstName,
                LastName = account.LastName,
                Email = account.Email,
                Phone = account.Phone,
            };

            return response;
        }
        
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateAccount(int Id, AccountModel accountModel)
        {
             var account = await _accountDbContext.Accounts.Where(x => x.Id == Id).FirstOrDefaultAsync();
            
            if (account == null)
            {
                return NotFound("Girdiğiniz bilgiler yanlış veya kayıt edilmemiş. Tekrar deneyiniz");
            }
               account.FirstName = accountModel.FirstName;
                account.LastName = accountModel.LastName;
                account.Email = accountModel.Email;
                account.Phone = accountModel.Phone;
                account.ModifiedDate = accountModel.ModifiedDate;

                _accountDbContext.Accounts.Update(account);
                await _accountDbContext.SaveChangesAsync();

                

            

            return Ok("Kullanıcı bilgileri güncellendi");
        }
    }
}
