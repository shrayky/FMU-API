using FmuApiDomain.MarkInformation.Entities;

namespace FmuApiDomain.MarkInformation.Models
{
    public class MarkSearchResult
    {
        public List<MarkEntity> Marks { get; set; } = new();
        public int Count { get; set; } = 0;
        public int CurrentPage { get; set; } = 0;
        public int PageSize { get; set; } = 0;
        public int TotalPages { get; set; } = 0;
        public string SearchTerm { get; set; } = string.Empty;
    }
}