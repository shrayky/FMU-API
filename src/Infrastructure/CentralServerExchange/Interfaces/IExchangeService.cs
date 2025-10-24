using CSharpFunctionalExtensions;
using FmuApiDomain.DTO.FmuApiExchangeData.Answer;
using FmuApiDomain.DTO.FmuApiExchangeData.Request;

namespace CentralServerExchange.Interfaces;

public interface IExchangeService
{
    Task<Result<FmuApiCentralResponse>> ActExchange(DataPacket request, string url);
    Task<Result<string>> DownloadNewConfiguration(string url);
    Task<Result> ConfirmDownloadConfiguration(string url);
    Task<Result<Stream>> DownloadSoftwareUpdate(string requestAddress);
}