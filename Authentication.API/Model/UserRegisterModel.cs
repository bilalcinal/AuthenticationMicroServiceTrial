using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core;

namespace Authentication.API.Model
{
    public class UserRegisterModel 
    {
        public string  UserName { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public string Password { get; set; }
    }
}