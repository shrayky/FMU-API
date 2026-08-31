using FmuApiApplication.Documents.Interfaces;
using FmuApiDomain.Attributes;
using FmuApiDomain.Documents;
using FmuApiDomain.Documents.Entities;
using FmuApiDomain.Documents.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Documents;

[AutoRegisterService(ServiceLifetime.Scoped)]
public class OfflineDocumentFlushService : IOfflineDocumentFlushService
{
    private readonly IOfflineDocumentStore _offlineStore;
    private readonly IDocumentRepository _documentRepository;
    private readonly IFrontolDocumentMarkStateService _markStateService;
    private readonly ILogger<OfflineDocumentFlushService> _logger;

    public OfflineDocumentFlushService(
        IOfflineDocumentStore offlineStore,
        IDocumentRepository documentRepository,
        IFrontolDocumentMarkStateService markStateService,
        ILogger<OfflineDocumentFlushService> logger)
    {
        _offlineStore = offlineStore;
        _documentRepository = documentRepository;
        _markStateService = markStateService;
        _logger = logger;
    }

    public async Task FlushAsync(CancellationToken cancellationToken)
    {
        var pending = await _offlineStore.ListPending();
        if (pending.Count == 0)
            return;

        _logger.LogInformation("Выгрузка {Count} документов из файловой очереди в CouchDB", pending.Count);

        foreach (var record in pending)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            await FlushOne(record);
        }
    }

    private async Task FlushOne(OfflineDocumentRecord record)
    {
        var uid = record.Document.Uid;

        try
        {
            if (string.IsNullOrWhiteSpace(uid))
            {
                await _offlineStore.Delete(uid);
                return;
            }

            switch (record.Status)
            {
                case OfflineDocumentStatus.Committed:
                    await _markStateService.ApplyAsync(record.Document);
                    await _documentRepository.Delete(uid);
                    await _offlineStore.Delete(uid);
                    _logger.LogInformation("Документ {Uid} из очереди закрыт в CouchDB", uid);
                    break;

                case OfflineDocumentStatus.Cancelled:
                    await _documentRepository.Delete(uid);
                    await _offlineStore.Delete(uid);
                    _logger.LogInformation("Документ {Uid} из очереди отменён в CouchDB", uid);
                    break;

                default:
                    var addResult = await _documentRepository.Add(record.Document);
                    if (addResult.IsFailure)
                    {
                        _logger.LogWarning("Документ {Uid} не выгружен в CouchDB: {Error}", uid, addResult.Error);
                        return;
                    }

                    await _offlineStore.Delete(uid);
                    _logger.LogInformation("Документ {Uid} из очереди записан в CouchDB", uid);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка выгрузки документа {Uid} из файловой очереди", uid);
        }
    }
}
