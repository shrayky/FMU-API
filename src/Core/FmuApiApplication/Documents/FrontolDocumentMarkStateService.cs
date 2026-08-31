using FmuApiApplication.Documents.Interfaces;
using FmuApiApplication.Mark.Interfaces;
using FmuApiDomain.Attributes;
using FmuApiDomain.Documents;
using FmuApiDomain.Mark.Enums;
using FmuApiDomain.Mark.Interfaces;
using FmuApiDomain.Mark.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace FmuApiApplication.Documents;

[AutoRegisterService(ServiceLifetime.Scoped)]
public class FrontolDocumentMarkStateService : IFrontolDocumentMarkStateService
{
    private const string SaleDocumentType = "receipt";

    private readonly IMarkFabric _markFabric;
    private readonly IMarkStateManager _markStateManager;

    public FrontolDocumentMarkStateService(IMarkFabric markFabric, IMarkStateManager markStateManager)
    {
        _markFabric = markFabric;
        _markStateManager = markStateManager;
    }

    /// <summary>
    /// Меняет состояние марок документа: продажа или возврат.
    /// </summary>
    public async Task ApplyAsync(RequestDocument beginDocument)
    {
        var state = beginDocument.Type == SaleDocumentType ? MarkState.Sold : MarkState.Returned;

        Dictionary<string, decimal> quantityByMark = beginDocument.Positions
            .SelectMany(p => p.Marking_codes.Select(code => new
            {
                code = Convert.FromBase64String(code),
                Quantity = p.Volume > 0 ? (decimal)p.Volume : (decimal)p.Quantity
            }))
            .ToDictionary(
                x => Encoding.UTF8.GetString(x.code),
                x => x.Quantity);

        var marks = await Task.WhenAll(
            quantityByMark.Keys.Select(code => _markFabric.Create(new(), code)));

        foreach (var mark in marks)
        {
            SaleData saleData = new()
            {
                CheckNumber = beginDocument.Number,
                SaleDate = DateTime.Now,
                Pos = beginDocument.Pos,
                IsSale = beginDocument.Type == SaleDocumentType,
                Quantity = quantityByMark[mark.Code]
            };

            await _markStateManager.ChangeState(mark.SGtin, state, saleData);
        }
    }
}
