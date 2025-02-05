namespace FmuApiDomain.Webix
{
    public class LogDataPacket
    {
        public List<string> FileNames { get; set; } = new();
        public string SelectedFile { get; set; } = string.Empty;
        public string Log {  get; set; } = string.Empty;
    }
}
