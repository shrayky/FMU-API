using FmuApiDomain.DTO.FmuApiExchangeData.DataPacket;

namespace FmuApiDomain.CentralServiceExchange.Interfaces;

public interface INodeInformationService
{
    Task<DataPacket> Create();
}