using FmuApiDomain.Configuration.Options;
using Microsoft.Data.Sqlite;

namespace Sqlite;

/// <summary>
/// Строка подключения к файлу SQLite по настройкам базы.
/// </summary>
public static class SqliteConnectionBuilder
{
    public static string Build(CouchDbConnection configuration)
    {
        return new SqliteConnectionStringBuilder
        {
            DataSource = configuration.ResolvedSqlitePath
        }.ToString();
    }
}
