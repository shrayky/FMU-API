using CSharpFunctionalExtensions;
using FmuApiDomain.Fmu.Document;

namespace FmuApiDomain.Fmu.PacketTrapper.Interfaces;

public interface IFmuPacketTrapper
{
    Task<Result> SaveCheckResultForCashRegister(RequestDocument requestDocument, FmuAnswer fmuAnswer);
}
