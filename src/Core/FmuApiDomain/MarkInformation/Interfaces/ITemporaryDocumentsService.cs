using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Frontol;

namespace FmuApiDomain.MarkInformation.Interfaces
{
    // TODO переименовать этот интерфейс в что то более понятное, что работает с документами
    public interface ITemporaryDocumentsService
    {
        Task<DocumentEntity> AddDocumentToDbAsync(RequestDocument data);
        Task<DocumentEntity> DocumentFromDbAsync(string uid);
        Task DeleteDocumentFromDbAsync(string uid);
        // TODO эта операция не для этого интерфейса, ее нужно перенести в отдельный
        Task<int> WareSaleOrganizationFromFrontolBaseAsync(string wareBarcode);
    }
}
