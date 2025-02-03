using ApplicationConfigurationService.Migrations;
using ApplicationConfigurationService.Settings;
using FmuApiDomain.Cache;
using FmuApiDomain.Configuration;
using FmuApiSettings;
using JsonSerialShared.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.FilesFolders;
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

            string configFolder = Folders.CommonApplicationDataFolder(ApplicationInformationConstants.Manufacture, ApplicationInformationConstants.AppName);

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

        private ApplicationSettings DefaultConfiguration()
        {
            ApplicationSettings settings = new();

            settings.NodeName = Environment.MachineName;
            settings.OrganisationConfig.FillIfEMpty();

            SaveConfiguration(settings);

            return settings;
        }

        private ApplicationSettings LoadConfiguration()
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

                var loadedConfiguration = JsonSerializer.Deserialize<ApplicationSettings>(jsonContent);

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

        private ApplicationSettings LoadBackupConfiguration()
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

            var loadedConfiguration = JsonSerializer.Deserialize<ApplicationSettings>(jsonContent);
          
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

        private ApplicationSettings CheckMigration(ApplicationSettings settings)
        {
            if (settings.AppVersion != ApplicationInformationConstants.AppVersion)
            {
                if (settings.AppVersion < 9)
                {
                    settings = MigrationTo9.DoMigration(settings);
                }

                settings.AppVersion = ApplicationInformationConstants.AppVersion;

                SaveConfiguration(settings);
            }

            return settings;
        }

        public void SaveConfiguration(ApplicationSettings settings)
        {
            _logger.LogWarning("Записываю данные в файл конфигурации");

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

        public async Task SaveConfigurationAsync(ApplicationSettings settings)
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

        private void CacheSettings(ApplicationSettings settings)
        {
            _cacheService.Set(_cacheKey, 
                              settings,
                              TimeSpan.FromMinutes(_cacheExpirationMinutes));
        }

        public ApplicationSettings GetSettings()
        {
            var settings = _cacheService.Get<ApplicationSettings>(_cacheKey);

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
            var settings = GetSettings();

            var parameters = new Parameters
            {
                AppVersion = ApplicationInformationConstants.AppVersion,
                Assembly = settings.Assembly,
                NodeName = settings.NodeName,
                ServerConfig = settings.ServerConfig,
                HostsToPing = settings.HostsToPing,
                MinimalPrices = settings.MinimalPrices,
                OrganisationConfig = settings.OrganisationConfig,
                FrontolAlcoUnit = settings.FrontolAlcoUnit,
                Database = settings.Database,
                TrueSignTokenService = settings.TrueSignTokenService,
                HttpRequestTimeouts = settings.HttpRequestTimeouts,
                Logging = settings.Logging,
                FrontolConnectionSettings = settings.FrontolConnectionSettings,
                SaleControlConfig = settings.SaleControlConfig,
                CentralServerConnectionSettings = settings.CentralServerConnectionSettings,
            };

            return parameters;
        }

        public async Task<Parameters> CurrentAsync()
        {
            return await Task.Run(() => { return Current(); });
        }

        public async Task UpdateAsync(Parameters parameters)
        {
            var settings = new ApplicationSettings
            {
                AppVersion = ApplicationInformationConstants.AppVersion,
                Assembly = parameters.Assembly,
                NodeName = parameters.NodeName,
                ServerConfig = parameters.ServerConfig,
                HostsToPing = parameters.HostsToPing,
                MinimalPrices = parameters.MinimalPrices,
                OrganisationConfig = parameters.OrganisationConfig,
                FrontolAlcoUnit = parameters.FrontolAlcoUnit,
                Database = parameters.Database,
                TrueSignTokenService = parameters.TrueSignTokenService,
                HttpRequestTimeouts = parameters.HttpRequestTimeouts,
                Logging = parameters.Logging,
                FrontolConnectionSettings = parameters.FrontolConnectionSettings,
                SaleControlConfig = parameters.SaleControlConfig,
                CentralServerConnectionSettings = parameters.CentralServerConnectionSettings,
            };

            await SaveConfigurationAsync(settings);

        }
    }
}



