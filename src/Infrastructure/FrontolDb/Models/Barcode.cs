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

        [Column("ASPECTVALUE1ID")]
        public int AspectValue1Id { get; set; }

        [Column("ASPECTVALUE2ID")]
        public int AspectValue2Id { get; set; }

        [Column("ASPECTVALUE3ID")]
        public int AspectValue3Id { get; set; }

        [Column("ASPECTVALUE4ID")]
        public int AspectValue4Id { get; set; }

        [Column("ASPECTVALUE5ID")]
        public int AspectValue5Id { get; set; }

        [Column("FACTOR")]
        public double Factor { get; set; }

        [Column("DELETED")]
        public int Deleted { get; set; }
    }
}
