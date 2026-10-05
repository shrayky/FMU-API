using CentralServerExchange.Interfaces;
using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.CentralServiceExchange.Models.Answer;
using FmuApiDomain.SoftwareUpdate.Interfaces;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace CentralServerExchange.Services;

[AutoRegisterService(ServiceLifetime.Singleton)]
public class SoftwareUpdateDownloadService
{
    private const int DownloadRetryCount = 3;
    private static readonly TimeSpan DownloadRetryDelay = TimeSpan.FromSeconds(5);

    private readonly ILogger<SoftwareUpdateDownloadService> _logger;
    private readonly IParametersService _parametersService;
    private readonly IExchangeService _exchangeService;
    private readonly IApplicationState _appState;
    private readonly ISoftwarePackageInstaller _packageInstaller;

    public SoftwareUpdateDownloadService(
        ILogger<SoftwareUpdateDownloadService> logger,
        IParametersService parametersService,
        IExchangeService exchangeService,
        IApplicationState appState,
        ISoftwarePackageInstaller packageInstaller)
    {
        _logger = logger;
        _parametersService = parametersService;
        _exchangeService = exchangeService;
        _appState = appState;
        _packageInstaller = packageInstaller;
    }

    public async Task<Result> DownloadAndInstall(FmuApiCentralResponse response, string baseAddress, string? bearerToken = null)
    {
        var parameters = await _parametersService.CurrentAsync();

        if (!parameters.FmuApiCentralServer.DownloadNewVersion
            || !_appState.IsOnline()
            || !response.SoftwareUpdateAvailable)
            return Result.Success();

        var now = TimeOnly.FromDateTime(DateTime.Now);
        if (!UpdateInstallSchedule.IsWithinSchedule(now, parameters.FmuApiCentralServer.SchedulerUpdateInstall))
        {
            _logger.LogInformation("Обновление отложено: текущее время вне разрешённых интервалов");
            return Result.Success();
        }

        _logger.LogInformation("Доступно обновление ПО в центральном сервере");

        var token = parameters.FmuApiCentralServer.Token;
        var sha256 = response.UpdateHash;

        if (string.IsNullOrWhiteSpace(sha256))
            return Result.Failure("Пустой UpdateHash для доступного обновления");

        if (sha256.Length != 64 || !sha256.All(Uri.IsHexDigit))
            return Result.Failure($"Некорректный формат UpdateHash: {sha256}");

        var requestAddress = string.IsNullOrEmpty(bearerToken)
            ? $"{baseAddress}/fmuApiUpdate/{token}"
            : $"{baseAddress}/fmuApiUpdate";

        var downloadResult = await DownloadAndVerifyAsync(requestAddress, sha256, bearerToken).ConfigureAwait(false);

        if (downloadResult.IsFailure)
        {
            _logger.LogError(downloadResult.Error);
            return Result.Failure(downloadResult.Error);
        }

        // Одновременные установки отсекает замок внутри установщика.
        var installResult = await _packageInstaller.InstallAsync(downloadResult.Value, sha256).ConfigureAwait(false);

        if (installResult.IsSuccess)
            return Result.Success();

        _logger.LogError(installResult.Error);
        return Result.Failure(installResult.Error);
    }

    private async Task<Result<string>> DownloadAndVerifyAsync(string requestAddress, string sha256, string? bearerToken)
    {
        var lastError = "Загрузка обновления не выполнялась";

        for (var attempt = 1; attempt <= DownloadRetryCount; attempt++)
        {
            var downloadResult = await _exchangeService
                .DownloadSoftwareUpdateToTemp(requestAddress, sha256, bearerToken)
                .ConfigureAwait(false);

            if (downloadResult.IsFailure)
            {
                lastError = downloadResult.Error;
                LogDownloadAttempt(attempt, lastError);

                if (attempt < DownloadRetryCount)
                    await Task.Delay(DownloadRetryDelay).ConfigureAwait(false);

                continue;
            }

            var fileName = downloadResult.Value;
            var checkResult = await CheckShaHash(fileName, sha256).ConfigureAwait(false);

            if (checkResult.IsSuccess)
                return Result.Success(PromotePartialToZip(fileName));

            TryDeleteFile(fileName);
            lastError = checkResult.Error;
            LogDownloadAttempt(attempt, lastError);

            if (attempt < DownloadRetryCount)
                await Task.Delay(DownloadRetryDelay).ConfigureAwait(false);
        }

        return Result.Failure<string>(lastError);
    }

    private void LogDownloadAttempt(int attempt, string error)
    {
        _logger.LogWarning(
            "Попытка {Attempt}/{Max} загрузки обновления не удалась: {Error}",
            attempt,
            DownloadRetryCount,
            error);
    }

    private static string PromotePartialToZip(string fileName)
    {
        if (!fileName.EndsWith(".partial", StringComparison.OrdinalIgnoreCase))
            return fileName;

        var zipPath = Path.ChangeExtension(fileName, ".zip");
        File.Move(fileName, zipPath, overwrite: true);
        return zipPath;
    }

    private async Task<Result> CheckShaHash(string filePath, string expectedSha256)
    {
        await using var fileStream = File.OpenRead(filePath);
        var hashBytes = await SHA256.HashDataAsync(fileStream).ConfigureAwait(false);
        var actualHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        if (string.Equals(actualHash, expectedSha256, StringComparison.OrdinalIgnoreCase))
            return Result.Success();

        var errorMessage = $"Хэш {actualHash} загруженного файла обновления не совпадает с ожидаемым {expectedSha256}";
        _logger.LogError(errorMessage);

        return Result.Failure(errorMessage);
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
    }
}
