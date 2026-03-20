using CentralServerExchange.Interfaces;
using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Constants;
using FmuApiDomain.DTO.FmuApiExchangeData.Answer;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;

namespace CentralServerExchange.Services;

[AutoRegisterService(ServiceLifetime.Singleton)]
public class SoftwareUpdateDownloadService
{
    private readonly ILogger<SoftwareUpdateDownloadService> _logger;
    private readonly IParametersService _parametersService;
    private readonly IExchangeService _exchangeService;
    private readonly IApplicationState _appState;
    
    private static readonly SemaphoreSlim UpdateLock = new(1, 1);

    public SoftwareUpdateDownloadService(ILogger<SoftwareUpdateDownloadService> logger, IParametersService parametersService, IExchangeService exchangeService, IApplicationState appState)
    {
        _logger = logger;
        _parametersService = parametersService;
        _exchangeService = exchangeService;
        _appState = appState;
    }

    public async Task<Result> DownloadAndInstall(FmuApiCentralResponse response, string baseAddress)
    {
        var parameters = await _parametersService.CurrentAsync();

        if (!parameters.FmuApiCentralServer.DownloadNewVersion 
            || !_appState.IsOnline() 
            || !response.SoftwareUpdateAvailable)
            return Result.Success();

        if (!await UpdateLock.WaitAsync(0))
            return Result.Failure("Обновление уже запущено");

        try
        {
            _logger.LogInformation("Доступно обновление ПО в центральном сервере");

            var token = parameters.FmuApiCentralServer.Token;
            var sha256 = response.UpdateHash;

            if (string.IsNullOrWhiteSpace(sha256))
                return Result.Failure("Пустой UpdateHash для доступного обновления");

            if (sha256.Length != 64 || !sha256.All(Uri.IsHexDigit))
                return Result.Failure($"Некорректный формат UpdateHash: {sha256}");

            var requestAddress = $"{baseAddress}/fmuApiUpdate/{token}";

            var downloadResult = await _exchangeService.DownloadSoftwareUpdateToTemp(requestAddress).ConfigureAwait(false);

            if (downloadResult.IsFailure)
            {
                _logger.LogError(downloadResult.Error);
                return Result.Failure(downloadResult.Error);
            }

            var fileName = downloadResult.Value;

            var checkResult = await CheckShaHash(fileName, sha256);

            if (checkResult.IsFailure)
            {
                try
                {
                    File.Delete(fileName);
                }
                catch
                {}

                _logger.LogError(checkResult.Error);
                return Result.Failure(checkResult.Error);
            }

            var installResult = InstallUpdate(fileName, sha256);

            if (!installResult.IsFailure)
                return Result.Success();
            
            _logger.LogError(installResult.Error);
            return Result.Failure(installResult.Error);
        }
        finally
        {
            UpdateLock.Release();
        }
    }

    private async Task<Result>  CheckShaHash(string filePath, string expectedSha256)
    {
        using var fileStream = File.OpenRead(filePath);
        using var downloadedSha256 = SHA256.Create();
        var hashBytes = await downloadedSha256.ComputeHashAsync(fileStream).ConfigureAwait(false);
        var actualHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
    
        fileStream.Position = 0;

        if (string.Equals(actualHash, expectedSha256, StringComparison.OrdinalIgnoreCase))
            return Result.Success();
        
        var errorMessage = $"Хэш {actualHash} загруженного файла обновления не совпадает с ожидаемым {expectedSha256}";
        _logger.LogError(errorMessage);
        
        return Result.Failure(errorMessage);
    }

    private Result InstallUpdate(string updateFileName, string sha256)
    {
        if (OperatingSystem.IsWindows())
            return UpdateWindowsApp(updateFileName, sha256);
        else if  (OperatingSystem.IsLinux())
            return UpdateLinuxApp(updateFileName);
        else
            return Result.Failure("Не поддерживаемая ОС");
    }

    private Result UpdateWindowsApp(string updateFileName, string sha256)
    {
        var installerPath = Path.Combine(Path.GetTempPath(), ApplicationInformation.AppName);

        if (!Directory.Exists(installerPath))
            Directory.CreateDirectory(installerPath);

        try
        {
            ZipFile.ExtractToDirectory(updateFileName, installerPath, true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Не удалось распаковать обновление!");
            
            return Result.Failure(ex.Message);
        }

        try
        {
            File.Delete(updateFileName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Не удалось удалить zip-архив с обновлением!");
            
            return Result.Failure(ex.Message);
        }
        
        
        Process process = new();
        ProcessStartInfo startInfo = new()
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            FileName = "cmd.exe",
            CreateNoWindow = true,
            Arguments = $"/c \"{installerPath}\\{ApplicationInformation.AppName}.exe\" --install --checksum {sha256}",
        };

        _logger.LogWarning("Найдено обновление, запускаю установку {arguments}.", startInfo.Arguments);
        
        process.StartInfo = startInfo;
        process.Start();
        process.WaitForExit();

        try
        {
            Directory.Delete(installerPath, true);
        }
        catch
        {
            _logger.LogWarning("Не удалось удалить временные файлы после установки обновления!");
        }

        return Result.Success();
    }

    private Result UpdateLinuxApp(string updateFileName)
    {
        _logger.LogWarning("Начинаю установку обновления");

        //const string appDirectory = $"/opt/{ApplicationInformation.Manufacture}/{ApplicationInformation.AppName}";
        //const string appFileName = $"{appDirectory}/fmu-api";
        //const string oldAppFileName = $"{appFileName}.old";
        //const string backUpFileName = $"{appFileName}.bkp";
        
        var installerPath = Path.Combine(Path.GetTempPath(), ApplicationInformation.AppName);

        try
        {
            ZipFile.ExtractToDirectory(updateFileName, installerPath, true);
            File.Delete(updateFileName);
        }
        catch (Exception e)
        {
            return Result.Failure($"Ошибка распаковки обновления в {installerPath}: {e.Message}"); 
        }

        Process process = new();
        ProcessStartInfo startInfo = new()
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            FileName = $"{Path.GetTempPath()}{ApplicationInformation.AppName}/{ApplicationInformation.AppName.ToLowerInvariant()}",
            CreateNoWindow = true,
            Arguments = "--install",
            RedirectStandardOutput = true,
        };
       
        process.StartInfo = startInfo;
        //var info = process.Start();
        //var outI = process.StandardOutput.ReadToEnd();
        process.Start();
        
        Task.Delay(TimeSpan.FromMinutes(15));

        return Result.Success();
    }
}

