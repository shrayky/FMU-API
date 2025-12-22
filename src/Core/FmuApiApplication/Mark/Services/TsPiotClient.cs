using FmuApiApplication.Mark.Interfaces;
using FmuApiApplication.Mark.Models;

namespace FmuApiApplication.Mark.Services;

public class TsPiotClient : ICheckOperation
{
    public async Task<MarkCheckResult> Check(string code, string sGtin, bool codeIsSGtin, int printGroupCode)
    {
        // https://tspiot.sandbox.crptech.ru/?mode=online&tab=marks
        throw new NotImplementedException();
    }
}