using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core;

namespace Authentication.API.Model
{
    public class UserRegisterModel 
    {
        public string  FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Password { get; set; }
        public string PaswordAgain { get; set; }
    }
}