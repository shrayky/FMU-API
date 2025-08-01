﻿using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Interface;
using FmuApiDomain.Frontol;
using FmuApiDomain.MarkInformation.Entities;
using FmuApiDomain.MarkInformation.Models;

namespace FmuApiDomain.MarkInformation.Interfaces
{
    public interface IMarkService
    {
        Task<DocumentEntity> AddDocumentToDbAsync(RequestDocument data);
        Task<DocumentEntity> DocumentFromDbAsync(string uid);
        Task DeleteDocumentFromDbAsync(string uid);
        Task<IMark> MarkAsync(string encodedMark);
        Task<MarkEntity> MarkChangeState(string id, string state, SaleData saleData);
        Task<MarkEntity> MarkInformationAsync(string id);
        Task<int> WareSaleOrganizationFromFrontolBaseAsync(string wareBarcode);
        Task<List<MarkEntity>> MarkInformationBulkAsync(List<string> list);
        Task DraftBeerUpdateAsync(string SGtin, int Quantity);
    }
}
