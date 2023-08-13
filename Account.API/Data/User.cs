﻿using System;
using Core;

namespace Account.API.Data
{
	public class User : BaseEntity
	{
        public string FirstName { get; set; }
		public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

    }
}

