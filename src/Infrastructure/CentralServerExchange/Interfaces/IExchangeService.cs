using CentralServerExchange.Dto.Answer;
using CentralServerExchange.Dto.Request;
using CSharpFunctionalExtensions;

namespace CentralServerExchange.Interfaces;

public interface IExchangeService
{
    Task<Result<FmuApiCentralResponse>> ActExchange(DataPacket request, string url);
}