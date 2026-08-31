using System.Text.Json;
using FmuApiDomain.Constants;
using FmuApiDomain.Documents;
using FmuApiDomain.Documents.Entities;
using FmuApiDomain.Documents.Interfaces;
using Microsoft.Extensions.Logging;
using Shared.FilesFolders;

namespace CouchDb.Services;

/// <summary>
/// Очередь документов во файлах рядом с конфигурацией приложения.
/// </summary>
public class FileOfflineDocumentStore : IOfflineDocumentStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly ILogger<FileOfflineDocumentStore> _logger;
    private readonly string _folder;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public FileOfflineDocumentStore(ILogger<FileOfflineDocumentStore> logger)
    {
        _logger = logger;
        _folder = Path.Combine(
            Folders.CommonApplicationDataFolder(ApplicationInformation.Manufacture, ApplicationInformation.AppName),
            "offline-documents");
    }

    public async Task Save(RequestDocument document, string status)
    {
        if (string.IsNullOrWhiteSpace(document.Uid))
            return;

        await _gate.WaitAsync();
        try
        {
            Directory.CreateDirectory(_folder);

            var record = new OfflineDocumentRecord
            {
                Document = document,
                Status = status
            };

            var json = JsonSerializer.Serialize(record, JsonOptions);
            var path = FilePath(document.Uid);
            var tempPath = path + ".tmp";

            await File.WriteAllTextAsync(tempPath, json);
            File.Move(tempPath, path, overwrite: true);

            _logger.LogWarning("Документ {Uid} сохранён в файловую очередь, статус {Status}", document.Uid, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось сохранить документ {Uid} в файловую очередь", document.Uid);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OfflineDocumentRecord?> Get(string uid)
    {
        if (string.IsNullOrWhiteSpace(uid))
            return null;

        await _gate.WaitAsync();
        try
        {
            var path = FilePath(uid);
            if (!File.Exists(path))
                return null;

            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<OfflineDocumentRecord>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось прочитать документ {Uid} из файловой очереди", uid);
            return null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task Delete(string uid)
    {
        if (string.IsNullOrWhiteSpace(uid))
            return;

        await _gate.WaitAsync();
        try
        {
            var path = FilePath(uid);
            if (!File.Exists(path))
                return;

            File.Delete(path);
            _logger.LogInformation("Документ {Uid} удалён из файловой очереди", uid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось удалить документ {Uid} из файловой очереди", uid);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<OfflineDocumentRecord>> ListPending()
    {
        await _gate.WaitAsync();
        try
        {
            if (!Directory.Exists(_folder))
                return [];

            var files = Directory.GetFiles(_folder, "*.json");
            var records = new List<OfflineDocumentRecord>(files.Length);

            foreach (var file in files)
            {
                var json = await File.ReadAllTextAsync(file);
                var record = JsonSerializer.Deserialize<OfflineDocumentRecord>(json, JsonOptions);
                if (record == null || string.IsNullOrWhiteSpace(record.Document.Uid))
                    continue;

                records.Add(record);
            }

            return records;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось прочитать файловую очередь документов");
            return [];
        }
        finally
        {
            _gate.Release();
        }
    }

    private string FilePath(string uid)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safeName = string.Concat(uid.Select(c => invalid.Contains(c) ? '_' : c));
        if (string.IsNullOrWhiteSpace(safeName))
            safeName = Guid.NewGuid().ToString("N");

        return Path.Combine(_folder, $"{safeName}.json");
    }
}
