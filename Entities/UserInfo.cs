using System.Reflection.Emit;

namespace AssinaAiApi.Entities
{
    public class UserInfo
    {
        public int Id { get; set; }
        public int PersonId { get; set; }
        public Person Person { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public Guid UniqueId { get; set; }        
    }
}
