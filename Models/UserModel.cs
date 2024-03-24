namespace AssinaAiApi.Models
{
    public class UserModel
    {
        public string Email { get; set; }
        public string Password { get; set; }

        public PersonModel Person { get;set; }
    }
}