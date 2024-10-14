using System.ComponentModel.DataAnnotations.Schema;
using FmuApiDomain.General;

namespace FmuApiDomain.Frontol
{
    [Table("SPRT")]
    public class Sprt : TableNameCode
    {
        [Column("MARK")]
        public string? Mark { get; set; }
        [Column("ISWARE")]
        public int IsWareI { get; set; }
        [Column("PARENTID")]
        public int? ParentId { get; set; } = 0;
        [Column("PRINTGROUPCLOSE")]
        public int? PrintGroupFiscalCheck { get; set; }
        public bool IsWare() => IsWareI == 1;
        public int FiscalPrinterGroupCode() => PrintGroupFiscalCheck ?? 0;
    }
}
