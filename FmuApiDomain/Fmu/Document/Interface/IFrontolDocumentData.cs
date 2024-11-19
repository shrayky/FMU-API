namespace FmuApiDomain.Fmu.Document.Interface
{
    public interface IFrontolDocumentData
    {
        public RequestDocument Document { get; set; }
        public string Id { get; set; }
        public string Rev {  get; set; }
        public bool Deleted { get; }
    }
}
