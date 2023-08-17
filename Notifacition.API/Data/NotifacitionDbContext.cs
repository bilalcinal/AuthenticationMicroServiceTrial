using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Notifacition.API.Data;

namespace Authentication.API.Data
{
    public class NotifacitionDbContext : DbContext
    {
      
      public NotifacitionDbContext(DbContextOptions<NotifacitionDbContext>options) : base(options)
       {
        
       }

        public DbSet<MailSettings> MailSettings { get; set; }

    }
}