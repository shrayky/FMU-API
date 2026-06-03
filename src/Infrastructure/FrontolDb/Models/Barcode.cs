using FmuApiDomain.Templates.Tables;
using System.ComponentModel.DataAnnotations.Schema;

namespace FrontolDb.Models
{
    [Table("BARCODE")]
    public class Barcode : IdField
    {
        [Column("WAREID")]
        public int WareId { get; set; }
        [Column("BARCODE")]
        public string? WareBarcode { get; set; }
    }
}
