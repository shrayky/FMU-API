using FmuApiDomain.Models.General;
using System.ComponentModel.DataAnnotations.Schema;

namespace FmuApiDomain.Models.Frontol
{
    [Table("BARCODE")]
    public class Barcode : TableWIthId
    {
        [Column("WAREID")]
        public int WareId { get; set; }
        [Column("BARCODE")]
        public string? WareBarcode { get; set; }
    }
}
