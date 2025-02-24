namespace FmuApiApplication.Mark.Interfaces
{
    public interface IMarkParser
    {
        string ParseCode(string markCode);
        string CalculateSGtin(string markCode);
        string CalculateCis(string markCode);
        string CalculateBarcode(string sgtin);
        string EncodeMark(string markingCode);
    }
}