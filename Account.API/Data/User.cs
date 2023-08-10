using System;
using Core;

namespace Account.API.Data
{
	public class User : BaseEntity
	{
		
        public string UserName { get; set; }

        public string Email { get; set; }

		public DateTime CreatedDate { get; set; }

	}
}

