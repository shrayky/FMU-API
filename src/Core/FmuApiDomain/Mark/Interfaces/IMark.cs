using CSharpFunctionalExtensions;
using FmuApiDomain.Documents;
using FmuApiDomain.Documents.Enums;
using FmuApiDomain.Mark.Entities;
using FmuApiDomain.Mark.Enums;
using FmuApiDomain.TrueApi.MarkData.Check;
using FmuApiDomain.TsPiot.Models;

namespace FmuApiDomain.Mark.Interfaces;

public interface IMark
{
    public string SGtin { get; }
    public string Barcode { get; }
    public string Code { get; }
    public string ErrorDescription { get; }
    public bool CodeIsSgtin { get; }
    public int AtolItemType { get; }
    public int TrueApiGroupId { get; }
    public MarkCheckSource CheckSource { get; }
    public Task<CheckMarksDataTrueApi> TrueApiData();
    public void SetPrintGroupCode(int code);
    public void SetPositionData(int itemType, string productName, int trueApiGroupId);
    public MarkEntity DatabaseState();
    public FmuAnswer MarkDataAfterCheck();
    public Task<Result<FmuAnswer>> PerformCheckAsync(OperationType operation);
    public void SetTsPiotSettings(TsPiotConnectionSettings tsPiotConnectionSettings);
}
