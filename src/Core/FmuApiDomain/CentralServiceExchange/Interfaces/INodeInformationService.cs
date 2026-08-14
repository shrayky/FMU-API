using FmuApiDomain.CentralServiceExchange.Models.DataPacket;

namespace FmuApiDomain.CentralServiceExchange.Interfaces;

public interface INodeInformationService
{
    Task<DataPacket> Create();
}