using FmuApiDomain.Templates.Tables;
using System.ComponentModel.DataAnnotations.Schema;

namespace FrontolDb.Models;

[Table("BEER_TAP")]
public class BeerTapEntity : IdField
{
    [Column("NAME")]
    public string Name { get; set; } = string.Empty;

    [Column("WAREID")]
    public int? WareId { get; set; }

    [Column("LABEL")]
    public string? MarkCode { get; set; } = string.Empty;

    [Column("VOLUME")]
    public double? Volume { get; set; }

    [Column("CODE")]
    public int TapCode { get; set; }

    [Column("WARECODE")]
    public int? WareCode { get; set; }

    [Column("WAREMARK")]
    public string? WareArticle { get; set; } = string.Empty;
}