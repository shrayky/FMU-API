using System.ComponentModel.DataAnnotations.Schema;

namespace FmuApiDomain.Templates.Tables
{
    public class IdField
    {
        [Column("ID")]
        public int Id { get; set; }
    }
}
