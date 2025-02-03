using FmuApiDomain.Webix;
using FmuApiSettings;
using Shared.FilesFolders;

namespace LogService
{
    public class LogInformationPacket
    {
        public static async Task<LogDataPacket> CollectLogs(string selectedLogFileName)
        {
            LogDataPacket defaultAnswer = new();

            string logFolderPath = string.Empty;

            if (OperatingSystem.IsWindows())
            {
                logFolderPath = Path.Combine(Folders.LogFolder(),
                                             ApplicationInformationConstants.Manufacture, ApplicationInformationConstants.AppName,
                                             "log");
            }
            else if (OperatingSystem.IsLinux())
            {
                logFolderPath = Path.Combine(Folders.LogFolder(),
                                             ApplicationInformationConstants.Manufacture.ToLower(),
                                             ApplicationInformationConstants.AppName.ToLower());
            }


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
                fileNameWithoutPrefix = Path.GetFileNameWithoutExtension(file).Replace(ApplicationInformationConstants.AppName.ToLower(), "");

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

            string templog = Path.Combine(Path.GetDirectoryName(uploadLogFileName), "templog.txt");

            try
            {
                File.Copy(uploadLogFileName, templog, true);
            }
            catch
            {
                return string.Empty;
            }

            string log = await File.ReadAllTextAsync(templog);

            File.Delete(templog);

            return log;
        }
    }
}
