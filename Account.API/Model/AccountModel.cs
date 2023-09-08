namespace Account.API.Model
{
    public class AccountModel
    {
      public int Id { get; set; }
      public string FirstName { get; set; }
      public string LastName { get; set; }
      public string Email { get; set; }
      public string Phone { get; set; }
      public DateTime ModifiedDate { get; set; }
    }
}