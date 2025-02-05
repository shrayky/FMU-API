namespace FmuApiDomain.Node.Models
{
    public sealed record NodeDataResponse
    {
        public bool ConfigurationUpdateAvailable { get; init; }
        public bool SoftwareUpdateAvailable { get; init; }
        public string? ErrorMessage { get; init; }
        private NodeDataResponse() { }

        public static NodeDataResponse Success(bool hasConfigurationUpdates, bool hasSoftwareUpdate)
        {
            return new()
            {
                ConfigurationUpdateAvailable = hasConfigurationUpdates,
                SoftwareUpdateAvailable = hasSoftwareUpdate,
                ErrorMessage = "ok"
            };
        }

        public static NodeDataResponse Error(string message)
        {
            return new()
            {
                ErrorMessage = message,
                ConfigurationUpdateAvailable = false,
                SoftwareUpdateAvailable = false
            };
        }

    }
}