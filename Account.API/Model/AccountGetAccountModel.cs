using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Account.API.Data;

namespace Account.API.Model
{
    public class AccountGetAccountModel
    {
      public int Id { get; set; }
      public string FirstName { get; set; }
      public string LastName { get; set; }
      public string Email { get; set; }
      public string Phone { get; set; }
    }
}