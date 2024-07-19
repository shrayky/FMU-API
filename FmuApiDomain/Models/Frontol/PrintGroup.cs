using System.ComponentModel.DataAnnotations.Schema;
using FmuApiDomain.Models.General;

namespace FmuApiDomain.Models.Frontol
{
    [Table("PRINTGROUP")]
    public class PrintGroup : TableNameCode
    {
        [Column("TEXT")]
        public string? Text { get; set; }
    }
}
