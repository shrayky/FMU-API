using FmuApiDomain.Cache;
using FmuApiSettings;
using Microsoft.Extensions.Logging;
using Shared.FilesFolders;
using System.Text.Json;
using TrueApiCdn.Interface;
using TrueApiCdn.Models;

namespace TrueApiCdn.Services
{
    public class SimpleCdnService : ICdnService
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<SimpleCdnService> _logger;

        private readonly string _cdnPath;
        private const string CACHE_KEY = "cdn_list";
        private readonly int _cacheExpirationMinutes = 60;

        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public SimpleCdnService(
            ICacheService cacheService,
            ILogger<SimpleCdnService> logger)
        {
            _cacheService = cacheService;
            _logger = logger;

            var configFolder = Folders.CommonApplicationDataFolder(ApplicationInformationConstants.Manufacture, ApplicationInformationConstants.AppName);
            _cdnPath = Path.Combine(configFolder, "cdn.json");
        }

        public async Task<IReadOnlyList<TrueSignCdn>> GetCdnsAsync()
        {
            var cdns = _cacheService.Get<List<TrueSignCdn>>(CACHE_KEY);

            if (cdns != null)
                return cdns;

            cdns = await LoadCdnsAsync();

            return cdns;
        }

        public async Task<TrueSignCdn?> GetActiveCdnAsync(int recursionCount = 0)
        {
            if (recursionCount > 1)
            {
                _logger.LogError("Превышено максимальное количество попыток получения активного CDN");
                return null;
            }

            var cdns = await GetCdnsAsync();

            var activeCdn = cdns
                .Where(cdn => !cdn.Offline)
                .OrderBy(cdn => cdn.Latency)
                .FirstOrDefault();

            if (activeCdn == null)
            {
                _logger.LogWarning("Все CDN офлайн, сбрасываем статус");
                await ResetOfflineStatusAsync();
                return await GetActiveCdnAsync(recursionCount + 1);
            }

            return activeCdn;
        }

        public async Task SaveCdnsAsync(IEnumerable<TrueSignCdn> cdns)
        {
            try
            {
                await _semaphore.WaitAsync();

                string? directoryPath = Path.GetDirectoryName(_cdnPath);

                if (directoryPath != null && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string jsonContent = JsonSerializer.Serialize(cdns);

                await File.WriteAllTextAsync(_cdnPath, jsonContent);

                CacheCdns(cdns.ToList());
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task UpdateCdnStatusAsync(string host, bool isOffline)
        {
            var cdns = (await GetCdnsAsync()).ToList();
            var cdn = cdns.FirstOrDefault(c => c.Host == host);

            if (cdn == null)
                return;


            if (isOffline)
                cdn.BringOffline();
            else
                cdn.BringOnline();

            await SaveCdnsAsync(cdns);
        }

        public async Task ResetOfflineStatusAsync()
        {
            var cdns = (await GetCdnsAsync()).ToList();

            foreach (var cdn in cdns)
            {
                cdn.BringOnline();
            }

            await SaveCdnsAsync(cdns);
        }

        public void InvalidateCache()
        {
            _cacheService.Remove(CACHE_KEY);
        }

        private async Task<List<TrueSignCdn>> LoadCdnsAsync()
        {
            if (!File.Exists(_cdnPath))
                return [];

            try
            {
                string jsonContent;

                try
                {
                    await _semaphore.WaitAsync();
                    jsonContent = await File.ReadAllTextAsync(_cdnPath);
                }
                finally
                {
                    _semaphore.Release();
                }

                await using FileStream fileStream = File.OpenRead(_cdnPath);
                var cdns = await JsonSerializer.DeserializeAsync<List<TrueSignCdn>>(fileStream);

                if (cdns == null)
                {
                    _logger.LogWarning("Файл CDN поврежден. Создаю новый пустой файл");
                    await SaveCdnsAsync([]);
                    return [];
                }

                CacheCdns(cdns);

                return cdns;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка чтения файла CDN. Создаю новый пустой файл");
                await SaveCdnsAsync([]);
                return [];
            }
        }

        private void CacheCdns(List<TrueSignCdn> cdns)
        {
            _cacheService.Set(CACHE_KEY, cdns, TimeSpan.FromMinutes(_cacheExpirationMinutes));
        }

    }
}
