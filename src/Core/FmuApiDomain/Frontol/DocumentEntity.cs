using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Templates.Tables;

namespace FmuApiDomain.Frontol
{
    public class DocumentEntity : IHaveStringId
    {
        public string Id { get; set; } = string.Empty;
        public RequestDocument FrontolDocument { get; set; } = new();
    }
}
