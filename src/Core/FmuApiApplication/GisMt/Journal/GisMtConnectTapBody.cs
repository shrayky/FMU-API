using System.Text.Json;
using System.Text.Json.Serialization;
using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.GisMt.Journal;

namespace FmuApiApplication.GisMt.Journal;

/// <summary>
/// Тело документа CONNECT_TAP для lk/documents/create.
/// </summary>
public static class GisMtConnectTapBody
{
    private const string DateFormat = "yyyy-MM-dd";

    public static string Build(GisMtJournalEntity document, GisMtParticipant participant)
    {
        var codes = document.Lines
            .Select(line => new ConnectTapCode
            {
                Cis = line.Cis,
                ConnectDate = (line.ConnectDate ?? document.DocDate).ToString(DateFormat)
            })
            .ToList();

        var body = new ConnectTapDocument
        {
            ParticipantInn = participant.Inn,
            ParticipantKpp = participant.IsIndividual ? null : participant.Kpp,
            FiasId = participant.IsIndividual ? participant.FiasId : null,
            Codes = codes
        };

        return JsonSerializer.Serialize(body);
    }

    private sealed class ConnectTapDocument
    {
        [JsonPropertyName("participantInn")]
        public string ParticipantInn { get; init; } = string.Empty;

        [JsonPropertyName("participantKpp")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ParticipantKpp { get; init; }

        [JsonPropertyName("fiasId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? FiasId { get; init; }

        [JsonPropertyName("codes")]
        public List<ConnectTapCode> Codes { get; init; } = [];
    }

    private sealed class ConnectTapCode
    {
        [JsonPropertyName("cis")]
        public string Cis { get; init; } = string.Empty;

        [JsonPropertyName("connectDate")]
        public string ConnectDate { get; init; } = string.Empty;
    }
}
