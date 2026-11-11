using CSharpFunctionalExtensions;
using FmuApiApplication.Mark.Interfaces;
using FmuApiDomain.Attributes;
using FmuApiDomain.BeerTaps.Models;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.GisMt.Journal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.GisMt.Journal;

/// <summary>
/// Акт постановки на кран по операции connect_tap от Фронтола.
/// </summary>
[AutoRegisterService(ServiceLifetime.Scoped)]
public class GisMtConnectTapService : IGisMtConnectTapService
{
    private readonly ILogger<GisMtConnectTapService> _logger;
    private readonly IParametersService _parametersService;
    private readonly IMarkParser _markParser;
    private readonly IGisMtJournalService _journalService;

    public GisMtConnectTapService(
        ILogger<GisMtConnectTapService> logger,
        IParametersService parametersService,
        IMarkParser markParser,
        IGisMtJournalService journalService)
    {
        _logger = logger;
        _parametersService = parametersService;
        _markParser = markParser;
        _journalService = journalService;
    }

    public async Task<Result> CreateFromTap(TapBeerOperation operation, CancellationToken cancellationToken = default)
    {
        var parameters = await _parametersService.CurrentAsync();
        var settings = parameters.GisMtSettings;

        if (!settings.DocumentsEnabled || !settings.AutoCreateConnectTap)
            return Result.Success();

        if (string.IsNullOrWhiteSpace(operation.Uid))
        {
            _logger.LogWarning("Постановка на кран без uid: акт журнала ГИС МТ не создаётся");

            return Result.Success();
        }

        using var uidLock = await GisMtJournalUidLock.Acquire(operation.Uid, cancellationToken);

        var document = new GisMtJournalEntity
        {
            Kind = GisMtJournalKinds.ConnectTap,
            Status = GisMtJournalStatuses.Draft,
            Uid = operation.Uid.Trim(),
            Inn = (operation.Position?.Organisation?.Inn ?? string.Empty).Trim(),
            DocDate = DateOnly.FromDateTime(DateTime.Now),
            CreatedAt = DateTime.Now
        };

        var mark = _markParser.EncodeMark(operation.Position?.MarkingCode ?? string.Empty);
        var cis = _markParser.CalculateCis(mark);

        document.Lines = [new GisMtJournalLine { Cis = cis, ConnectDate = document.DocDate }];

        var organisation = GisMtOrganisationResolver.Find(parameters, document.Inn);

        if (organisation is null)
        {
            document.ErrorText = document.Inn.Length == 0
                ? "Документ Фронтола не содержит ИНН организации"
                : $"Организация с ИНН {document.Inn} не найдена в настройках";
        }
        else
        {
            document.Kpp = organisation.Kpp;
            document.FiasId = organisation.FiasId;

            if (cis.Length == 0)
            {
                document.ErrorText = "КИ марки не удалось вычислить, документ в Честный знак не отправляется";
            }
            else
            {
                var participant = GisMtParticipantRules.Resolve(document.Inn, document.Kpp, document.FiasId);

                if (participant.IsFailure)
                    document.ErrorText = participant.Error;
            }
        }

        if (document.ErrorText.Length > 0)
            document.Status = GisMtJournalStatuses.Error;

        var created = await _journalService.Create(document);

        if (created.IsFailure)
            return created;

        if (created.Value.Status == GisMtJournalStatuses.Error)
            return Result.Success();

        if (!settings.AutoSendConnectTap)
            return Result.Success();

        var sent = await _journalService.Send(created.Value.Id, cancellationToken);

        if (sent.IsFailure)
            _logger.LogWarning("Акт постановки на кран {Id} не отправлен: {Error}", created.Value.Id, sent.Error);

        return Result.Success();
    }
}
