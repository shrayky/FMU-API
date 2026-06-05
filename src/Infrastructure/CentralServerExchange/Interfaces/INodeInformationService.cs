using FmuApiDomain.DTO.FmuApiExchangeData.DataPacket;

namespace CentralServerExchange.Interfaces;

public interface INodeInformationService
{
    Task<DataPacket> Create();
}