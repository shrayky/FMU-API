using System.ComponentModel.DataAnnotations.Schema;

namespace TableTemplates
{
    public class IdField
    {
        [Column("ID")]
        public int Id { get; set; }
    }
}
