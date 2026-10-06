using CSharpFunctionalExtensions;
using FmuApiDomain.Database.Models;

namespace FmuApiDomain.Database.Interfaces;

/// <summary>
/// Перенос данных из CouchDB в SQLite: запуск и чтение хода.
/// </summary>
public interface ICouchDbToSqliteImport
{
    const string AlreadyRunningMessage = "Перенос уже выполняется";

    /// <summary>
    /// Запускает перенос в фоне. Повторный запуск во время переноса — неуспех.
    /// </summary>
    Result Start();

    /// <summary>
    /// Текущее состояние хода переноса.
    /// </summary>
    CouchDbImportState State();
}
