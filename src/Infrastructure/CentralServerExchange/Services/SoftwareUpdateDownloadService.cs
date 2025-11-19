using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using CentralServerExchange.Interfaces;
using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Constants;
using FmuApiDomain.DTO.FmuApiExchangeData.Answer;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        
            var requestAddress = $"{baseAddress}/fmuApiUpdate/{token}";

            var prepareUpdate = await DownloadSoftware(requestAddress)
                .Bind(async fileStream => await CheckShaHash(fileStream, sha256))
                .Bind(async fileStream => await SaveToTemp(fileStream));

            if (prepareUpdate.IsFailure)
            {
                _logger.LogError(prepareUpdate.Error);
                return Result.Failure(prepareUpdate.Error);
            }

            var installResult = InstallUpdate(prepareUpdate.Value);

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

    private async Task<Result<Stream>> DownloadSoftware(string requestAddress)
    {
        var downloadResult = await _exchangeService.DownloadSoftwareUpdate(requestAddress).ConfigureAwait(false);
        
        return downloadResult.IsFailure ? Result.Failure<Stream>(downloadResult.Error) : Result.Success(downloadResult.Value);
    }

    private async Task<Result<Stream>> CheckShaHash(Stream fileStream, string expectedSha256)
    {
        using var downloadedSha256 = SHA256.Create();
        var hashBytes = await downloadedSha256.ComputeHashAsync(fileStream).ConfigureAwait(false);
        var actualHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
    
        fileStream.Position = 0;

        if (string.Equals(actualHash, expectedSha256))
            return Result.Success(fileStream);
        
        var errorMessage = $"Хэш {actualHash} загруженного файла обновления не совпадает с ожидаемым {expectedSha256}";
        _logger.LogError(errorMessage);
        
        await fileStream.DisposeAsync();
        return Result.Failure<Stream>(errorMessage);
    }

    private async Task<Result<string>> SaveToTemp(Stream stream)
    {
        var tmpFolder = Path.Combine(Path.GetTempPath(), ApplicationInformation.AppName);
        var filePath = Path.Combine(tmpFolder, "update.zip");

        try
        {
            if (!Directory.Exists(tmpFolder))
                Directory.CreateDirectory(tmpFolder);

            await using var fileStream = File.Create(filePath);
            await stream.CopyToAsync(fileStream);

            _logger.LogInformation("Обновление загружено в: {FilePath}", filePath);

            return Result.Success(filePath);
        }
        catch (Exception e)
        {
            var errMsg = $"Ошибка копирования скачанного файла обновления в {filePath}: {e.Message}";
            _logger.LogError(errMsg);
            return Result.Failure<string>(errMsg);
        }
    }

    private Result InstallUpdate(string updateFileName)
    {
        if (OperatingSystem.IsWindows())
            return UpdateWindowsApp(updateFileName);
        else if  (OperatingSystem.IsLinux())
            return UpdateLinuxApp(updateFileName);
        else
            return Result.Failure("Не поддерживаемая ОС");
    }

    private Result UpdateWindowsApp(string updateFileName)
    {
        var installerPath = Path.Combine(Path.GetTempPath(), ApplicationInformation.AppName);

        if (!Directory.Exists(installerPath))
            Directory.CreateDirectory(installerPath);

        ZipFile.ExtractToDirectory(updateFileName, installerPath, true);
        File.Delete(updateFileName);
        
        Process process = new();
        ProcessStartInfo startInfo = new()
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            FileName = "cmd.exe",
            CreateNoWindow = true,
            Arguments = $"/c {installerPath}\\{ApplicationInformation.AppName}.exe --install",
            RedirectStandardOutput = true,
        };

        _logger.LogWarning("Найдено обновление, запускаю установку {arguments}.", startInfo.Arguments);
        
        process.StartInfo = startInfo;
        process.Start();
        
        Task.Delay(TimeSpan.FromMinutes(5));
        
        //Directory.Delete(installerPath, true);

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

