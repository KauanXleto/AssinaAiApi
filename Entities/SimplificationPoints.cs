using System.Reflection.Emit;

namespace AssinaAiApi.Entities
{
    public class SimplificationPoints
    {
        public int Id { get; set; }
        public int SimpliticationId { get; set; }
        public string Name { get; set; }
        public string Result { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public Guid UniqueId { get; set; }
    }
}
