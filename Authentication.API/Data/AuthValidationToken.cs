using Core;

namespace Authentication.API.Data
{
    public class AuthValidationToken : BaseEntity
    {
        public int AccountId { get; set; }
        public string Token { get; set; }
        public bool Used { get; set; }
        public DateTime Expires { get; set; }
    }
}