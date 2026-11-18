using FmuApiDomain.Templates.Tables;
using System.ComponentModel.DataAnnotations.Schema;

namespace FrontolDb.Models;

/// <summary>
/// Остаток товара Frontol: связь штрихкода с ценой по товару и разрезам.
/// </summary>
[Table("REMAIN")]
public class Remain : IdField
{
    [Column("WAREID")]
    public int WareId { get; set; }

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

    [Column("DELETED")]
    public int Deleted { get; set; }

    [Column("ENTERPRISEID")]
    public int EnterpriseId { get; set; }

    [Column("PRICETYPE")]
    public int? PriceType { get; set; }
}
