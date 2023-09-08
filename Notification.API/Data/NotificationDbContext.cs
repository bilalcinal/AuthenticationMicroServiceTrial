using Microsoft.EntityFrameworkCore;

namespace Notification.API.Data
{
    public class NotificationDbContext : DbContext
    {
        public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options){}
        public DbSet<MailSettings> MailSettings { get; set; }
    }
}
