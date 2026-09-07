using CSharpFunctionalExtensions;
using FmuApiDomain.Documents;

namespace FmuApiDomain.PacketTrapper.Interfaces;

public interface IFmuPacketTrapper
{
    Task<Result<string>> SaveCheckResultForCashRegister(RequestDocument requestDocument, FmuAnswer fmuAnswer);
}
