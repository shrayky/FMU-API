using FmuApiDomain.Fmu.Document.Interface;
using FmuApiDomain.MarkInformation.Entities;
using FmuApiDomain.MarkInformation.Models;

namespace FmuApiDomain.MarkInformation.Interfaces
{
    public interface IMarkService
    {
        public Task<IFrontolDocumentData> AddDocumentToDbAsync(IFrontolDocumentData data);
        public Task<IFrontolDocumentData> DocumentFromDbAsync(string uid);
        public Task DeleteDocumentFromDbAsync(string uid);
        public Task<IMark> MarkAsync(string encodedMark);
        public Task<MarkEntity> MarkChangeState(string id, string state, SaleData saleData);
        public Task<MarkEntity> MarkInformationAsync(string id);
        public Task<int> WareSaleOrganizationFromFrontolBaseAsync(string wareBarcode);
    }
}
