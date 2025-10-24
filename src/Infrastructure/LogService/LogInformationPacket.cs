using FmuApiDomain.Constants;
using FmuApiDomain.Webix;
using Shared.FilesFolders;

namespace LogService
{
    public abstract class LogInformationPacket
    {
        public static async Task<LogDataPacket> CollectLogs(string selectedLogFileName)
        {
            LogDataPacket defaultAnswer = new();

            var logFolderPath = Folders.LogFolder(ApplicationInformation.Manufacture, ApplicationInformation.AppName);

            if (!Directory.Exists(logFolderPath))
                return defaultAnswer;

            var files = Directory.EnumerateFiles(logFolderPath, "fmu-api*.log");

            var enumerable = files.ToList();
            
            if (enumerable.Count == 0)
                return defaultAnswer;

            var uploadLogFileName = string.Empty;
            var nowFileName = string.Empty;
            var fileNameWithoutPrefix = string.Empty;

            foreach (var file in enumerable)
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

            var directoryName = Path.GetDirectoryName(uploadLogFileName);
            
            if (directoryName == null)
                return string.Empty;
            
            var tempLog = Path.GetTempFileName();

            try
            {
                File.Copy(uploadLogFileName, tempLog, true);
            }
            catch
            {
                return string.Empty;
            }

            var log = await File.ReadAllTextAsync(tempLog);

            File.Delete(tempLog);

            return log;
        }
    }
}
