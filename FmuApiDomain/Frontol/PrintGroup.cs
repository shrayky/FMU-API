using System.ComponentModel.DataAnnotations.Schema;
using TableTemplates;

namespace FmuApiDomain.Frontol
{
    [Table("PRINTGROUP")]
    public class PrintGroup : NameCodeFields
    {
        [Column("TEXT")]
        public string? Text { get; set; }
    }
}
