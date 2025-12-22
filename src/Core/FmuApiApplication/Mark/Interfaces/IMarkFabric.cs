using FmuApiDomain.Fmu.Document;
using FmuApiDomain.MarkInformation.Interfaces;

namespace FmuApiApplication.Mark.Interfaces;

public interface IMarkFabric
{
    Task<IMark> Create(Position position, string mark);
}