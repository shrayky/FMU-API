using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Templates.Tables;

namespace FmuApiDomain.Database.Dto
{
    public class DocumentEntity : IHaveStringId
    {
        public string Id { get; set; } = string.Empty;
        public RequestDocument FrontolDocument { get; set; } = new();
    }
}
