using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace Authentication.API.Security.Encyption;

    public class SecurityKeyHelper
    {
        public static SecurityKey CreateSecurityKey(string security)
		{
			return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(security));
		}
    }
