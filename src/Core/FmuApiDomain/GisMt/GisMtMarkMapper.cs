using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.GisMt.Models;
using FmuApiDomain.MarkInformation.Entities;
using FmuApiDomain.MarkInformation.Enums;
using FmuApiDomain.MarkInformation.Models;
using FmuApiDomain.TrueApi.MarkData;
using FmuApiDomain.TrueApi.MarkData.Check;

namespace FmuApiDomain.GisMt;

/// <summary>
/// Маппинг ответа cises/info в сущность остатка марки.
/// </summary>
public static class GisMtMarkMapper
{
    private static readonly HashSet<string> SoldStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "RETIRED",
        "WRITTEN_OFF"
    };

    /// <summary>
    /// Создаёт или обновляет сущность марки из ответа TrueAPI.
    /// </summary>
    public static GisMtMarkEntity FromCisInfo(
        CisInfoData info,
        string organisationInn,
        string sourceDocumentId,
        DateTime infoLoadedAtUtc)
    {
        var cis = !string.IsNullOrWhiteSpace(info.Cis) ? info.Cis! : info.RequestedCis ?? string.Empty;
        var gtin = info.Gtin ?? string.Empty;
        // printView из TrueAPI — предпочтительный sGTIN; иначе считаем из КИ
        var sgtin = !string.IsNullOrWhiteSpace(info.PrintView)
            ? info.PrintView!
            : GisMtSgtinCalculator.Calculate(cis, gtin);

        return new GisMtMarkEntity
        {
            Id = sgtin,
            SGtin = sgtin,
            Cis = cis,
            Gtin = gtin,
            OwnerInn = info.OwnerInn ?? string.Empty,
            OwnerName = info.OwnerName ?? string.Empty,
            ProducerInn = info.ProducerInn ?? string.Empty,
            Status = info.Status ?? string.Empty,
            Sold = IsSold(info),
            ExpireDate = ParseExpireDate(info),
            ProductGroup = info.ProductGroup ?? string.Empty,
            ProductGroupId = info.ProductGroupId,
            IsTracking = false,
            SourceDocumentId = sourceDocumentId,
            OrganisationInn = organisationInn,
            InfoLoadedAt = infoLoadedAtUtc
        };
    }

    /// <summary>
    /// Определяет признак продажи/выбытия по статусу КИ.
    /// </summary>
    public static bool IsSold(CisInfoData info)
    {
        if (info.MarkWithdraw)
            return true;

        if (string.IsNullOrWhiteSpace(info.Status))
            return false;

        return SoldStatuses.Contains(info.Status);
    }

    private static DateTime? ParseExpireDate(CisInfoData info)
    {
        if (info.ExpireDate.HasValue)
            return info.ExpireDate;

        if (string.IsNullOrWhiteSpace(info.ExpirationDate))
            return null;

        if (DateTime.TryParse(info.ExpirationDate, out var parsed))
            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);

        if (long.TryParse(info.ExpirationDate, out var unix) && unix > 0)
            return DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;

        return null;
    }

    /// <summary>
    /// Преобразует марку остатка ГИС МТ в формат проверки TrueAPI.
    /// </summary>
    public static CodeDataTrueApi ToCodeData(GisMtMarkEntity entity)
    {
        var inCirculation = string.Equals(entity.Status, "INTRODUCED", StringComparison.OrdinalIgnoreCase);

        return new CodeDataTrueApi
        {
            Cis = entity.Cis,
            Gtin = entity.Gtin,
            PrintView = entity.SGtin,
            Found = true,
            Valid = !entity.IsExpired,
            Utilised = true,
            Verified = true,
            Realizable = inCirculation && !entity.Sold,
            IsOwner = true,
            Sold = entity.Sold,
            ExpireDate = entity.ExpireDate,
            IsTracking = entity.IsTracking,
            ProducerInn = entity.ProducerInn,
            GroupIds = entity.ProductGroupId > 0 ? [entity.ProductGroupId] : null
        };
    }

    /// <summary>
    /// Преобразует марку остатка ГИС МТ в сущность fmu-api-marks для проверки/продажи.
    /// </summary>
    public static MarkEntity ToMarkEntity(GisMtMarkEntity entity)
    {
        var codeData = ToCodeData(entity);

        return new MarkEntity
        {
            Id = entity.Id,
            MarkId = entity.SGtin,
            State = entity.Sold ? MarkState.Sold : MarkState.Stock,
            TrueApiCisData = codeData,
            TrueApiAnswerProperties = new TrueApiAnswerData
            {
                Code = 0,
                Description = "Данные из остатков ГИС МТ",
                ReqId = "gis-mt-stock"
            }
        };
    }

    /// <summary>
    /// Формирует ответ TrueAPI из марки остатка ГИС МТ.
    /// </summary>
    public static CheckMarksDataTrueApi ToCheckMarksData(GisMtMarkEntity entity)
    {
        return new CheckMarksDataTrueApi
        {
            Code = 0,
            Description = "Данные из остатков ГИС МТ",
            ReqId = "gis-mt-stock",
            Codes = [ToCodeData(entity)]
        };
    }
}
