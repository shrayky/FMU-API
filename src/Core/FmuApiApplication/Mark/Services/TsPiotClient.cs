using FmuApiApplication.Mark.Interfaces;
using FmuApiApplication.Mark.Models;

namespace FmuApiApplication.Mark.Services;

public class TsPiotClient : ICheckOperation
{
    public async Task<MarkCheckResult> Check(string code, string sGtin, bool codeIsSGtin, int printGroupCode)
    {
        throw new NotImplementedException();
    }
}