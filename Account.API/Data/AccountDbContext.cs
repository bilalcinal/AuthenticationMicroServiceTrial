using Microsoft.EntityFrameworkCore;

namespace Account.API.Data
{
    public class AccountDbContext : DbContext
    {
        public AccountDbContext(DbContextOptions<AccountDbContext>options) : base(options)
       {
        
       }

       public DbSet<Account> Account {get; set;}

    }
}