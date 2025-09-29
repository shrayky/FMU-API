using FmuApiDomain.DTO.FmuApiExchangeData.Request;

namespace CentralServerExchange.Interfaces;

public interface INodeInformationService
{
    Task<DataPacket> Create();
}