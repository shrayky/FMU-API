using FmuApiDomain.Templates.Tables;
using System.ComponentModel.DataAnnotations.Schema;

namespace FmuApiDomain.Frontol
{
    [Table("PRINTGROUP")]
    public class PrintGroup : NameCodeFields
    {
        [Column("TEXT")]
        public string? Text { get; set; }
    }
}
