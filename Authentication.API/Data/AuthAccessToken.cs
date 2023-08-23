﻿using Core;
//Burası kalkacak AuthValidationa gidicek
//mailden linke tıklanınca otomatik jwt üretip login olacak
namespace Authentication.API.Data
{
    public class AuthAccessToken : BaseEntity
    {
        public int AccountId { get; set; }
        public string Token { get; set; }
        public DateTime Expires { get; set; }
    }
}
