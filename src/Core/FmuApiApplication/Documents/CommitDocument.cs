using CSharpFunctionalExtensions;
using FmuApiApplication.Documents.Interfaces;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Documents;
using FmuApiDomain.Documents.Interfaces;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Documents;

public class CommitDocument : IFrontolDocumentService
{
    private RequestDocument Document { get; set; }
    private Lazy<IDocumentRepository> TemporaryDocumentsService { get; set; }
    private Lazy<IOfflineDocumentStore> OfflineDocumentStore { get; set; }
    private Lazy<IFrontolDocumentMarkStateService> MarkStateService { get; set; }
    private IParametersService ParametersService { get; set; }
    private IApplicationState AppState { get; set; }
    private ILogger<CommitDocument> Logger { get; set; }

    private readonly Parameters _configuration;

    private CommitDocument(RequestDocument requestDocument, IServiceProvider provider)
    {
        Document = requestDocument;

        TemporaryDocumentsService = new Lazy<IDocumentRepository>(provider.GetRequiredService<IDocumentRepository>);
        OfflineDocumentStore = new Lazy<IOfflineDocumentStore>(provider.GetRequiredService<IOfflineDocumentStore>);
        MarkStateService = new Lazy<IFrontolDocumentMarkStateService>(provider.GetRequiredService<IFrontolDocumentMarkStateService>);

        AppState = provider.GetRequiredService<IApplicationState>();
        ParametersService = provider.GetRequiredService<IParametersService>();
        Logger = provider.GetRequiredService<ILogger<CommitDocument>>();
        _configuration = ParametersService.Current();
    }

    private static CommitDocument CreateObject(RequestDocument requestDocument, IServiceProvider provider)
        => new(requestDocument, provider);

    public static IFrontolDocumentService Create(RequestDocument requestDocument, IServiceProvider provider)
        => CreateObject(requestDocument, provider);

    public async Task<Result<FmuAnswer>> ActionAsync()
    {
        var sendResult = await SendDocumentToAlcoUnit();

        if (sendResult.IsFailure)
            return Result.Failure<FmuAnswer>(sendResult.Error);

        return await CommitDocumentAsync();
    }

    private async Task<Result<FmuAnswer>> CommitDocumentAsync()
    {
        FmuAnswer checkResult = new();

        if (!_configuration.Database.ConfigurationIsEnabled)
            return Result.Success(checkResult);

        var beginDocument = await LoadBeginDocument();

        if (beginDocument == null)
        {
            Logger.LogWarning(
                "Документ {Uid} не найден в CouchDB и в файловой очереди, commit принят без смены статусов марок",
                Document.Uid);
            return Result.Success(checkResult);
        }

        if (!AppState.CouchDbOnline())
        {
            await OfflineDocumentStore.Value.Save(beginDocument, OfflineDocumentStatus.Committed);
            return Result.Success(checkResult);
        }

        await MarkStateService.Value.ApplyAsync(beginDocument);
        await TemporaryDocumentsService.Value.Delete(Document.Uid);
        await OfflineDocumentStore.Value.Delete(Document.Uid);

        return Result.Success(checkResult);
    }

    private async Task<RequestDocument?> LoadBeginDocument()
    {
        if (AppState.CouchDbOnline())
        {
            var loadResult = await TemporaryDocumentsService.Value.Get(Document.Uid);
            if (loadResult.IsSuccess)
                return loadResult.Value.FrontolDocument;
        }

        var offline = await OfflineDocumentStore.Value.Get(Document.Uid);
        return offline?.Document;
    }

    private async Task<Result> SendDocumentToAlcoUnit()
    {
        if (string.IsNullOrEmpty(_configuration.FrontolAlcoUnit.NetAdres))
            return Result.Success();

        await Task.Delay(1);

        var auDoc = Document;

        return Result.Success(auDoc);
    }
}
