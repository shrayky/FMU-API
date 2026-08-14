using FmuApiDomain.Documents;
using FmuApiDomain.Mark.Interfaces;

namespace FmuApiApplication.Mark.Interfaces;

public interface IMarkFabric
{
    Task<IMark> Create(Position position, string mark);
}