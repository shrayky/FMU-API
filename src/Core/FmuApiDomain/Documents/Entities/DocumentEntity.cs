using FmuApiDomain.Documents;
using FmuApiDomain.Templates.Tables;

namespace FmuApiDomain.Documents.Entities
{
    public class DocumentEntity : IHaveStringId
    {
        public string Id { get; set; } = string.Empty;
        public RequestDocument FrontolDocument { get; set; } = new();
    }
}
