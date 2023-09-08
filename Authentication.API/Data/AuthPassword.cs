using Core;

namespace Authentication.API.Data
{
    public class AuthPassword : BaseEntity
    {
        public int AccountId { get; set; }
        public byte[] PasswordSalt { get; set; }
        public byte[] PasswordHash { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}