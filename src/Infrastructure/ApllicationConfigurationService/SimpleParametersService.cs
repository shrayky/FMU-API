using ApplicationConfigurationService.Migrations;
using FmuApiDomain.Cache.Interfaces;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.FilesFolders;
using Shared.Json;
using System.Text.Json;

namespace ApplicationConfigurationService
{
    public class SimpleParametersService : IParametersService
    {
        private readonly ILogger<SimpleParametersService> _logger;
        private readonly ICacheService _cacheService;
        private readonly IServiceProvider _services;

        private readonly string _configPath = string.Empty;
        private readonly string _configBackUpPath = string.Empty;
        private readonly string _cacheKey = "app_settings";
        private readonly int _cacheExpirationMinutes = 600;
        private static readonly object _lock = new();
        
        private static readonly SemaphoreSlim _semaphore = new(1, 1);

        public SimpleParametersService(IServiceProvider services)
        {
            _services = services;
            _logger = _services.GetRequiredService<ILogger<SimpleParametersService>>();
            _cacheService = _services.GetRequiredService<ICacheService>();

            string configFolder = Folders.CommonApplicationDataFolder(ApplicationInformation.Manufacture, ApplicationInformation.AppName);

            _configPath = Path.Combine(configFolder, "config.json");
            _configBackUpPath = Path.Combine(configFolder, "config.bkp");

            InitializeConfiguration();

        }

        private void InitializeConfiguration()
        {
            if (!File.Exists(_configPath))
                DefaultConfiguration();
            else
                LoadConfiguration();
        }

        private Parameters DefaultConfiguration()
        {
            Parameters settings = new();

            settings.NodeName = Environment.MachineName;
            settings.OrganisationConfig.FillIfEmpty();

            SaveConfiguration(settings);

            return settings;
        }

        private Parameters LoadConfiguration()
        {
            try
            {
                string jsonContent;
                try
                {
                    _semaphore.Wait();
                    jsonContent = File.ReadAllText(_configPath);
                }
                finally
                {
                    _semaphore.Release();
                }

                var loadedConfiguration = JsonSerializer.Deserialize<Parameters>(jsonContent);

                if (loadedConfiguration == null)
                {
                    if (File.Exists(_configBackUpPath))
                    {
                        _logger.LogWarning("Файл конфигурации поврежден или пуст. Пробую загрузить резервную копию");
                        return LoadBackupConfiguration();
                    }
                    else
                    { 
                        _logger.LogWarning("Файл конфигурации поврежден или пуст. Создаю новый файл с настройками по умолчанию");
                        return DefaultConfiguration();
                    }
                }

                loadedConfiguration = CheckMigration(loadedConfiguration);
                CacheSettings(loadedConfiguration);

                return loadedConfiguration;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка чтения файла конфигурации. Создаю новый файл с настройками по умолчанию");
                return DefaultConfiguration();
            }
        }

        private Parameters LoadBackupConfiguration()
        {
            string jsonContent;
          
            try
            {
                _semaphore.Wait();
                jsonContent = File.ReadAllText(_configBackUpPath);
            }
            finally
            {
                _semaphore.Release();
            }

            var loadedConfiguration = JsonSerializer.Deserialize<Parameters>(jsonContent);
          
            if (loadedConfiguration == null)
            {
                _logger.LogWarning("Резервная копия конфигурации повреждена. Создаю новый файл с настройками по умолчанию");
                return DefaultConfiguration();
            }

            _logger.LogInformation("Конфигурация успешно загружена из резервной копии");
           
            loadedConfiguration = CheckMigration(loadedConfiguration);
            CacheSettings(loadedConfiguration);

            // Восстанавливаем основной файл из резервной копии
            SaveConfiguration(loadedConfiguration);

            return loadedConfiguration;
        }

        private Parameters CheckMigration(Parameters settings)
        {
            if (settings.AppVersion != ApplicationInformation.AppVersion)
            {
                if (settings.AppVersion < 9)
                {
                    settings = MigrationTo9.DoMigration(settings);
                }

                settings.AppVersion = ApplicationInformation.AppVersion;

                SaveConfiguration(settings);
            }

            return settings;
        }

        public void SaveConfiguration(Parameters settings)
        {
            _logger.LogWarning("Записываю данные в файл конфигурации");

            settings.AppVersion = ApplicationInformation.AppVersion;
            settings.Assembly = ApplicationInformation.Assembly;

            lock (_lock)
            {
                string? directoryPath = Path.GetDirectoryName(_configPath);

                if (directoryPath == null)
                    return;

                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string jsonContent = JsonSerializer.Serialize(settings, JsonSerializeOptionsProvider.Default());

                File.WriteAllText(_configPath, jsonContent);

                CacheSettings(settings);
            }
        }

        public async Task SaveConfigurationAsync(Parameters settings)
        {
            _logger.LogWarning("Записываю данные в файл конфигурации");

            try
            {
                await _semaphore.WaitAsync();

                string? directoryPath = Path.GetDirectoryName(_configPath);
                if (directoryPath == null)
                    return;

                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string jsonContent = JsonSerializer.Serialize(settings, JsonSerializeOptionsProvider.Default());

                await File.WriteAllTextAsync(_configBackUpPath, jsonContent);
                await File.WriteAllTextAsync(_configPath, jsonContent);

                CacheSettings(settings);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private void CacheSettings(Parameters settings)
        {
            _cacheService.Set(_cacheKey, 
                              settings,
                              TimeSpan.FromMinutes(_cacheExpirationMinutes));
        }

        public Parameters GetSettings()
        {
            var settings = _cacheService.Get<Parameters>(_cacheKey);

            if (settings != null)
                return settings;

            return LoadConfiguration();
        }

        public void InvalidateCache()
        {
            try
            {
                _logger.LogInformation("Invalidating configuration cache");
                _cacheService.Remove(_cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating configuration cache");
                throw;
            }
        }

        public bool IsCacheAvailable()
        {
            try
            {
                // Пробуем получить любое значение из кэша
                var isAvailable = _cacheService.Get<string>("health_check") != null;

                if (!isAvailable)
                {
                    // Пробуем записать тестовое значение
                    _cacheService.Set("health_check", "ok", TimeSpan.FromSeconds(1));
                    isAvailable = true;
                }

                _logger.LogDebug("Cache availability check: {IsAvailable}", isAvailable);
                return isAvailable;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache is not available");
                return false;
            }
        }

        public Parameters Current()
        {
            return GetSettings();
        }

        public async Task<Parameters> CurrentAsync()
        {
            return await Task.Run(() => { return Current(); });
        }

        public async Task UpdateAsync(Parameters parameters)
        {
            await SaveConfigurationAsync(parameters);

        }
    }
}



