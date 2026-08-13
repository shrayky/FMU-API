using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.PacketTrapper.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FmuPacketTrapper.Services;

[AutoRegisterService(ServiceLifetime.Scoped)]
public class FmuPacketTrapper : IFmuPacketTrapper
{
    private readonly ILogger<FmuPacketTrapper> _logger;
    private readonly IParametersService _appParameters;

    private readonly string _folderPath = string.Empty;
    private readonly bool _saveDocument = false;

    public FmuPacketTrapper(ILogger<FmuPacketTrapper> logger, IParametersService appParameters)
    {
        _logger = logger;
        _appParameters = appParameters;

        var appSettings = _appParameters.Current();
        _folderPath = appSettings.SaleControlConfig.MarkCheckResultSave.Directory;
        _saveDocument = appSettings.SaleControlConfig.MarkCheckResultSave.Enable;
    }

    public async Task<Result> SaveCheckResultForCashRegister(RequestDocument requestDocument, FmuAnswer fmuAnswer)
    {
        if (!_saveDocument)
            return Result.Success();

        if (fmuAnswer.Truemark_response.Codes.Count == 0)
        {
            var err = "FmuPacketTrapper - нечего сохранять для ккт - пустой ответ от fmuapi";
            _logger.LogError(err);
            return Result.Failure(err);
        }

        var position = requestDocument.Positions.FirstOrDefault();

        if (position == null)
        {
            var err = "FmuPacketTrapper - нечего сохранять для ккт - пустой запрос от КПО";
            _logger.LogError(err);
            return Result.Failure(err);
        }

        var markCode = position.Marking_codes.FirstOrDefault();

        if (markCode == null)
        {
            var err = "FmuPacketTrapper - нечего сохранять для ккт - пустой запрос от КПО";
            _logger.LogError(err);
            return Result.Failure(err);
        }

        var checkResult = $"{fmuAnswer.Truemark_response.ReqId} {fmuAnswer.Truemark_response.ReqTimestamp}";

        return await SaveResultToFile(checkResult, markCode);
    }

    private async Task<Result> SaveResultToFile(string checkResult, string markCode)
    {
        try
        {
            if (!Directory.Exists(_folderPath))
                Directory.CreateDirectory(_folderPath);

            if (!Directory.Exists(_folderPath))
            {
                var err = "FmuPacketTrapper - несущестаует каталог для выгрузки результатов проверки в файл\"";
                _logger.LogError(err);
                return Result.Failure(err);
            }

            var fileName = Path.Combine(_folderPath, string.Concat(markCode, ".txt"));


            StreamWriter file = new(fileName, false);
            await file.WriteAsync(checkResult);
            file.Close();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }

        return Result.Success();
    }
}
