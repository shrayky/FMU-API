using System.ComponentModel.DataAnnotations.Schema;

namespace FmuApiDomain.Models.General
{
    public class TableWIthId
    {
        [Column("ID")]
        public int Id { get; set; }
    }
}
