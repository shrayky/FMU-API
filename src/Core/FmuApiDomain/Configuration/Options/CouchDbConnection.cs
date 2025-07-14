
using System.Text.Json.Serialization;

namespace FmuApiDomain.Configuration.Options
{
    public class CouchDbConnection
    {
        public bool Enable { get; set; } = false;
        public string NetAddress { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int BulkBatchSize { get; set; } = 1000;
        public int BulkParallelTasks { get; set; } = 4;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? NetAdres { get; set; } = string.Empty;
        
        //Эти поля сохранены для совместимости со старыми версиями, сейчас имена баз жестко прописаны в коде
        public string MarksStateDbName { get; set; } = string.Empty;
        public string FrontolDocumentsDbName { get; set; } = string.Empty;
        public string AlcoStampsDbName { get; set; } = string.Empty;

        [JsonIgnore]
        public bool ConfigurationIsEnabled => !string.IsNullOrWhiteSpace(NetAddress) &&
                                                !string.IsNullOrWhiteSpace(UserName) &&
                                                !string.IsNullOrWhiteSpace(Password) &&
                                                Enable;

        [JsonIgnore]
        public bool DatabaseCheckIsEnabled => ConfigurationIsEnabled && Enable;

    }
}
