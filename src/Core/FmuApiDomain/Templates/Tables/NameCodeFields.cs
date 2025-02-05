using System.ComponentModel.DataAnnotations.Schema;

namespace FmuApiDomain.Templates.Tables
{
    public class NameCodeFields : IdField
    {
        [Column("CODE")]
        public int Code { get; set; }
        [Column("NAME")]
        public string? Name { get; set; }
    }
}
