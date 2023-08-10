using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core;

namespace Authentication.API.Data
{
    public class UserRegister : BaseEntity
    {
        public string UserName { get; set; }

        public string Email { get; set; }

		public DateTime CreatedDate { get; set; }
        
    }
}