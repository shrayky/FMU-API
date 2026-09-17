using System.Text.Json;
using CouchDb.DatabaseScheme;
using Xunit;

namespace FmuApiApplication.Tests;

public class DatabaseIndexesTests
{
    [Fact]
    public void MarksDb_не_содержит_индекс_по_всему_data()
    {
        var indexes = DatabaseIndexes.DatabaseIndexSchema()[DatabaseNames.MarksDbName];

        Assert.DoesNotContain(indexes, index => index.Name == "mark-data-idx");
        Assert.Contains(indexes, index => index.Name == "mark-id-idx");
        Assert.Contains(indexes, index => index.Name == "timeStamp-data-idx");
    }

    [Fact]
    public void ObsoleteIndexes_удаляет_лишние_json_индексы_кроме_all_docs()
    {
        var schema = DatabaseIndexes.DatabaseIndexSchema()[DatabaseNames.MarksDbName];
        var existing = new[]
        {
            new CouchDbIndexEntry { Name = "_all_docs", Type = "special", Ddoc = string.Empty },
            new CouchDbIndexEntry { Name = "mark-id-idx", Type = "json", Ddoc = "_design/mark-id-idx" },
            new CouchDbIndexEntry { Name = "mark-data-idx", Type = "json", Ddoc = "_design/abc123" }
        };

        var obsolete = DatabaseIndexes.ObsoleteIndexes(schema, existing);

        Assert.Single(obsolete);
        Assert.Equal("mark-data-idx", obsolete[0].Name);
    }

    [Fact]
    public void IndexDefinition_в_json_пишет_ddoc_равный_имени()
    {
        var json = JsonSerializer.Serialize(new CouchDbIndexDefinition("mark-id-idx", new(["data.markId"])));

        using var document = JsonDocument.Parse(json);
        Assert.Equal("mark-id-idx", document.RootElement.GetProperty("name").GetString());
        Assert.Equal("mark-id-idx", document.RootElement.GetProperty("ddoc").GetString());
    }
}
