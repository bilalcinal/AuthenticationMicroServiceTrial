using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Authentication.API.Security.Encyption;

    public class SecurityKeyHelper
    {
        public static SecurityKey CreateSecurityKey(string security)
		{
			return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(security));
		}
    }
