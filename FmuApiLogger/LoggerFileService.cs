using FmuApiDomain.Interfaces.Services;
using FmuApiSettings;

namespace FmuApiLogger
{
    public class LoggerFileService : IFileService
    {
        private readonly string _filePath = string.Concat(Constants.DataFolderPath, "fmu-api.log");
        public void Write(string message)
        {
            using (StreamWriter writer = new(_filePath, true, System.Text.Encoding.UTF8))
            {
                writer.WriteLine(message);
            }
        }

        public async Task WriteAsync(string message)
        {
            using (StreamWriter writer = new(_filePath, true, System.Text.Encoding.UTF8))
            {
                await writer.WriteLineAsync(message);
            }
        }
    }
}
