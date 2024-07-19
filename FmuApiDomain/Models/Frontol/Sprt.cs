using System.ComponentModel.DataAnnotations.Schema;
using FmuApiDomain.Models.General;

namespace FmuApiDomain.Models.Frontol
{
    [Table("SPRT")]
    public class Sprt : TableNameCode
    {
        [Column("MARK")]
        public string? Mark { get; set; }
        [Column("ISWARE")]
        private int IsWareI {  get; set; }
        [Column("PARENTID")]
        public int ParentId {  get; set; }
        [Column("PRINTGROUPCLOSE")]
        public int PrintGroupForCheck { get; set; }
        public bool IsWare() => (IsWareI == 1);
    }
}
