using FmuApiDomain.Templates.Tables;
using System.ComponentModel.DataAnnotations.Schema;

namespace FrontolDb.Models;

[Table("PRINTGROUP")]
public class PrintGroup : NameCodeFields
{
    [Column("TEXT")]
    public string? Text { get; set; }
}
