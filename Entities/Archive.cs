using System.Reflection.Emit;

namespace AssinaAiApi.Entities
{
    public class Archive
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string Extension { get; set; }
        public decimal FileSize { get; set; }
        public string Path { get; set; }
        public string Base64 { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public Guid UniqueId { get; set; }
    }
}
