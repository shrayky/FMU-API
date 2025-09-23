using FmuApiDomain.Constants;
using FmuApiDomain.Webix;
using Shared.FilesFolders;

namespace LogService
{
    public class LogInformationPacket
    {
        public static async Task<LogDataPacket> CollectLogs(string selectedLogFileName)
        {
            LogDataPacket defaultAnswer = new();

            var logFolderPath = string.Empty;

            logFolderPath = Path.Combine(Folders.LogFolder(),
                ApplicationInformation.Manufacture,
                ApplicationInformation.AppName,
                "log");

            if (!Directory.Exists(logFolderPath))
                return defaultAnswer;

            var files = Directory.EnumerateFiles(logFolderPath, "fmu-api*.log");

            if (!files.Any())
                return defaultAnswer;

            string uploadLogFileName = string.Empty;
            string nowFileName = string.Empty;
            string fileNameWithoutPrefix = string.Empty;

            foreach (var file in files)
            {
                fileNameWithoutPrefix = Path.GetFileNameWithoutExtension(file).Replace(ApplicationInformation.AppName.ToLower(), "");

                defaultAnswer.FileNames.Add(fileNameWithoutPrefix);

                if (selectedLogFileName == string.Empty)
                    continue;

                if (fileNameWithoutPrefix == selectedLogFileName)
                    uploadLogFileName = file;

                nowFileName = file;
            }

            if (selectedLogFileName == "now")
            {
                uploadLogFileName = nowFileName;
                selectedLogFileName = fileNameWithoutPrefix;
            }

            defaultAnswer.Log = await ExtractLogDataAsync(uploadLogFileName);
            defaultAnswer.SelectedFile = selectedLogFileName;

            return defaultAnswer;
        }

        private static async Task<string> ExtractLogDataAsync(string uploadLogFileName)
        {
            if (string.IsNullOrEmpty(uploadLogFileName))
                return string.Empty;

            string tempLog = Path.Combine(Path.GetDirectoryName(uploadLogFileName), "temp_slog.txt");

            try
            {
                File.Copy(uploadLogFileName, tempLog, true);
            }
            catch
            {
                return string.Empty;
            }

            string log = await File.ReadAllTextAsync(tempLog);

            File.Delete(tempLog);

            return log;
        }
    }
}
