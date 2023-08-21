using Core;

namespace Authentication.API.Data
{
    public class AuthAccessToken : BaseEntity
    {
        public int AccountId { get; set; }
        public string Token { get; set; }
        public DateTime Expires { get; set; }
    }
}
