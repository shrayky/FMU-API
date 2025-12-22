using CSharpFunctionalExtensions;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.TrueApi.MarkData.Check;
using FmuApiDomain.MarkInformation.Entities;
using FmuApiDomain.Fmu.Document.Enums;

namespace FmuApiDomain.MarkInformation.Interfaces
{
    public interface IMark
    {
        public string SGtin { get; }
        public string Barcode { get; }
        public string Code { get; }
        public string ErrorDescription { get; }
        public bool CodeIsSgtin { get; }
        public Task<CheckMarksDataTrueApi> TrueApiData();
        public void SetPrintGroupCode(int code);
        public MarkEntity DatabaseState();
        public FmuAnswer MarkDataAfterCheck();
        public Task<Result<FmuAnswer>> PerformCheckAsync(OperationType operation);
        public void SetTsPiotSettings(TsPiotConnectionSettings tsPiotConnectionSettings);
    }
}
