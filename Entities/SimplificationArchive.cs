using System.Reflection.Emit;

namespace AssinaAiApi.Entities
{
    public class SimplificationArchive
    {
        public int Id { get; set; }
        public int SimpliticationId { get; set; }
        public int ArchiveId { get; set; }
        public Archive Archive { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public Guid UniqueId { get; set; }
    }
}
