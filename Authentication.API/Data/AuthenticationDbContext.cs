using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Authentication.API.Data
{
    public class AuthenticationDbContext : DbContext
    {
         public AuthenticationDbContext(DbContextOptions<AuthenticationDbContext>options) : base(options)
       {
        
       }

       public DbSet<UserRegister> UserRegister { get; set; }
        
    }
}