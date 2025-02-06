namespace FmuApiDomain.LocalModule.Models
{
    public class StatusLm
    {
        public long LastSync { get; set; } = 0;
        public string Version { get; set; } = string.Empty;
        public string Inst {  get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status {  get; set; } = string.Empty;
        public string OperationMode { get; set; } = string.Empty;
        public string RequiresDownload { get; set; } = string.Empty;
    }
}
