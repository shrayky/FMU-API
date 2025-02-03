using CSharpFunctionalExtensions;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.TrueSignApi.MarkData.Check;

namespace FmuApiDomain.MarkInformation.Interfaces
{
    public interface IMark
    {
        public string SGtin { get; }
        public string Barcode { get; }
        public string Code { get; }
        public string ErrorDescription { get; }
        public bool CodeIsSgtin { get; }
        public CheckMarksDataTrueApi TrueApiData();
        public void SetPrintGroupCode(int code);
        public MarkInformation DatabaseState();
        public FmuAnswer MarkDataAfterCheck();
        public Task<Result<FmuAnswer>> PerformCheckAsync();
    }
}
