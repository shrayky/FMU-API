using System.ComponentModel.DataAnnotations.Schema;

namespace FmuApiDomain.General
{
    public class TableWIthId
    {
        [Column("ID")]
        public int Id { get; set; }
    }
}
