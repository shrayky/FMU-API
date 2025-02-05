using System.ComponentModel.DataAnnotations.Schema;

namespace TableTemplates
{
    public class NameCodeFields : IdField
    {
        [Column("CODE")]
        public int Code { get; set; }
        [Column("NAME")]
        public string? Name { get; set; }
    }
}
