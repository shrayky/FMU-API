using System.Text.Json.Serialization;

namespace FmuApiDomain.Configuration.Options;

/// <summary>
/// Подключения к базам Frontol и выбор базы справочника товаров.
/// </summary>
public record ConnectedFrontolDatabases
{
    private int? _printGroupSourseId;

    public SyncBeerTaps SyncBeerTapsSettings { get; set; } = new();

    public int FrontolWareDataSourceId { get; set; }

    [Obsolete("Используйте FrontolWareDataSourceId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? PrintGroupSourseId
    {
        get => _printGroupSourseId;
        set
        {
            _printGroupSourseId = value;
            if (FrontolWareDataSourceId == 0 && value is > 0)
                FrontolWareDataSourceId = value.Value;
        }
    }

    public List<FrontolConnectionSettings> ConnectionSettings { get; set; } = [];

    public int ResolveWareDataSourceId()
        => FrontolWareDataSourceId != 0 ? FrontolWareDataSourceId : _printGroupSourseId ?? 0;
}

public record SyncBeerTaps
{
    public bool SyncBeerTapsEnabled { get; set; }
    public int SyncBeerTapsPeriodSeconds { get; set; } = 30;
}
