using FmuApiApplication.Mark.Models;

namespace FmuApiApplication.Mark.Interfaces;

public interface ICheckOperation
{
    Task<MarkCheckResult> Check(string code, string sGtin, bool codeIsSGtin, int printGroupCode);
}