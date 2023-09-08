using Microsoft.EntityFrameworkCore;

namespace Authentication.API.Data
{
   public class AuthenticationDbContext : DbContext
    {
      public AuthenticationDbContext(DbContextOptions<AuthenticationDbContext>options) : base(options){}
      public DbSet<AuthPassword> AuthPasswords { get; set; }
      public DbSet<AuthAccessToken> AuthAccessTokens { get; set; }
      public DbSet<AuthValidationToken> AuthValidationTokens { get; set; }
    }
   
}