using CentralServerExchange.Dto;
using CentralServerExchange.Dto.Request;

namespace CentralServerExchange.Interfaces;

public interface INodeInformationService
{
    Task<DataPacket> Create();
}