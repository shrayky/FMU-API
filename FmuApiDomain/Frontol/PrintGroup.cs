using System.ComponentModel.DataAnnotations.Schema;
using FmuApiDomain.General;

namespace FmuApiDomain.Frontol
{
    [Table("PRINTGROUP")]
    public class PrintGroup : TableNameCode
    {
        [Column("TEXT")]
        public string? Text { get; set; }
    }
}
