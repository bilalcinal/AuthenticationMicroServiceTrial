using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core;

namespace Authentication.API.Data
{
    public class UserPassword : BaseEntity
    {
         public byte[] PasswordSalt { get; set; }
        public byte[] PasswordHash { get; set; }
        
    }
}