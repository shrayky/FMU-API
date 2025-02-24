namespace FmuApiDomain.Fmu.Document.Interface
{
    public interface IFrontolDocumentData
    {
        RequestDocument Document { get; set; }
        string Id { get; set; }
        string Rev {  get; set; }
        bool Deleted { get; }
    }
}
