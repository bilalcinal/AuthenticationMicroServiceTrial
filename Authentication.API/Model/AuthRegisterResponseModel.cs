namespace Authentication.API.Model
{
    public class AuthRegisterResponseModel
    {
        public string Token { get; set; }
        public string Success { get; set; }
        public string Error { get; set; }
    }
}