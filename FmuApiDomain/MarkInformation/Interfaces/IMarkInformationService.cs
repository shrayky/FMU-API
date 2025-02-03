using FmuApiDomain.Fmu.Document.Interface;

namespace FmuApiDomain.MarkInformation.Interfaces
{
    public interface IMarkInformationService
    {
        public Task<IFrontolDocumentData> AddDocumentToDbAsync(IFrontolDocumentData data);
        public Task<IFrontolDocumentData> DocumentFromDbAsync(string uid);
        public Task DeleteDocumentFromDbAsync(string uid);
        public Task<IMark> MarkAsync(string encodedMark);
        public Task<MarkInformation> MarkChangeState(string id, string state, SaleData saleData);
        public Task<MarkInformation> MarkInformationAsync(string id);
        public Task<int> WareSaleOrganizationFromFrontolBaseAsync(string wareBarcode);
    }
}
