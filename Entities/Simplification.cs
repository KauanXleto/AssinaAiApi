using System.Reflection.Emit;

namespace AssinaAiApi.Entities
{
    public class Simplification
    {
        public int Id { get; set; }
        public int PersonId { get; set; }
        public Person Person { get; set; }
        public string Name { get; set; }
        public string OriginalTextFile { get; set; }
        public string Resume { get; set; }

        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public Guid UniqueId { get; set; }

        public IList<SimplificationPoints> SimplificationPoints { get; set; }
    }
}
