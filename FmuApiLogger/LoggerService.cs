using FmuApiDomain.Interfaces.Logger;
using FmuApiDomain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace FmuApiLogger
{
    public class LoggerService<T> : IloggerService<T>
    {
        private readonly IFileService _fileService;
        private readonly string _nameSpace;

        public LoggerService(IFileService fileService) 
        {
            _fileService = fileService;
            _nameSpace = typeof(T).Name;
        }

        private string CastMessage(string message, LogLevel logLevel, short traceId = 0, Exception? exception = null)
            => exception != null
                ? $"[{DateTime.Now}] - {traceId} {_nameSpace} {logLevel} {message} {exception}"
                : $"[{DateTime.Now}] - {traceId} {_nameSpace} {logLevel} {message}";

        public void Log(LogLevel level, string message, short traceId = 0, Exception? exception = null)
        {
            string logMsg = CastMessage(message, LogLevel.Error, traceId, exception);

            switch (level)
            {
                case LogLevel.Information:
                    LogInformation(logMsg);
                    break;

                case LogLevel.Warning:
                    LogWarning(logMsg);
                    break;

                case LogLevel.Error:
                    LogError(logMsg, exception);
                    break;

                case LogLevel.Critical:
                    LogCritical(logMsg, exception);
                    break;

                default:
                    Trace(logMsg);
                    break;
            }
        }

        public void LogInformation(string message)
        {
            _fileService.Write(message);
        }

        public void LogWarning(string message)
        {
            _fileService.Write(message);
        }

        public void Trace(string message)
        {
            _fileService.Write(message);
        }

        public void LogError(string message, Exception? exception)
        {
            _fileService.Write(message);
        }

        public void LogCritical(string message, Exception? exception)
        {
            _fileService.Write(message);
        }
    }
}
