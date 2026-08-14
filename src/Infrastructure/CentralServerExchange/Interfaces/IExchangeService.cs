using CSharpFunctionalExtensions;
using FmuApiDomain.CentralServiceExchange.Models.Answer;
using FmuApiDomain.CentralServiceExchange.Models.DataPacket;

namespace CentralServerExchange.Interfaces;

public interface IExchangeService
{
    Task<Result<FmuApiCentralResponse>> ActExchange(DataPacket request, string url);
    Task<Result<string>> DownloadNewConfiguration(string url);
    Task<Result> ConfirmDownloadConfiguration(string url);
    Task<Result<string>> DownloadSoftwareUpdateToTemp(string requestAddress);
}