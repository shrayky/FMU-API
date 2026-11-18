using FmuApiDomain.Templates.Tables;
using System.ComponentModel.DataAnnotations.Schema;

namespace FrontolDb.Models;

/// <summary>
/// Цена товара Frontol на остаток, с датой начала действия.
/// </summary>
[Table("PRICEDATA")]
public class PriceData : IdField
{
    [Column("PRICE")]
    public double Price { get; set; }

    [Column("ACTDATETIME")]
    public DateTime ActDateTime { get; set; }

    [Column("REMAINID")]
    public int RemainId { get; set; }
}
