using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Account.API.Data;
using Account.API.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
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
        public async Task<AccountGetAccountModel> CreateAccount(AccountModel accountModel)
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
        public async Task<IActionResult> UpdateAccount(AccountModel accountModel)
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

                var account = await _accountDbContext.Accounts
                    .Where(x => x.Email == userEmail)
                    .FirstOrDefaultAsync();

                if (account == null)
                {
                    return NotFound("Kullanıcı bulunamadı veya yetkisiz işlem.");
                }
                account.FirstName = accountModel.FirstName;
                account.LastName = accountModel.LastName;
                account.Email = accountModel.Email;
                account.Phone = accountModel.Phone;
                account.ModifiedDate = DateTime.UtcNow; // Güncelleme tarihi değiştiriliyor

                _accountDbContext.Accounts.Update(account);
                await _accountDbContext.SaveChangesAsync();

                // Jwt oluşturuyoruz
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

                return Ok(new { Token = newTokenString, Message = "Kullanıcı bilgileri güncellendi" });


            }
            catch (Exception ex)
            {
                // Hata yönetimi burada yapılabilir
                return StatusCode(500, "Sunucu hatası");
            }
        }

    }
}
         

