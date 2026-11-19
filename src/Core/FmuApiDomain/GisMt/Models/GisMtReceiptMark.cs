namespace FmuApiDomain.GisMt.Models;

/// <summary>
/// Марка для акта списания: количество и цену оператор может поправить на ТСД.
/// </summary>
public record GisMtReceiptMark(string Mark, decimal? Quantity = null, decimal? Price = null);
