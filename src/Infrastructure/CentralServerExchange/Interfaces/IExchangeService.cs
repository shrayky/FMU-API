using CSharpFunctionalExtensions;
using FmuApiDomain.CentralServiceExchange.Models;
using FmuApiDomain.CentralServiceExchange.Models.Answer;
using FmuApiDomain.CentralServiceExchange.Models.DataPacket;

namespace CentralServerExchange.Interfaces;

public interface IExchangeService
{
    Task<Result<AgentAccessToken>> Handshake(string url, string token, string secret);
    Task<Result<FmuApiCentralResponse>> ActExchange(DataPacket request, string url, string? bearerToken = null);
    Task<Result<string>> DownloadNewConfiguration(string url, string? bearerToken = null);
    Task<Result> ConfirmDownloadConfiguration(string url, string? bearerToken = null);
    Task<Result<string>> DownloadSoftwareUpdateToTemp(string requestAddress, string sha256, string? bearerToken = null);
}