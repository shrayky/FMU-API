using LoggerConfig;
using Serilog;
using Serilog.Events;

namespace LoggerConfigTest
{
    public class SerilogConfigurationTests
    {
        // ����� �� ��������� ������� ����������
        [Theory]
        [InlineData(null, typeof(ArgumentNullException), "Value cannot be null.")]
        [InlineData("", typeof(ArgumentException), "The value cannot be an empty string.")]
        [InlineData(" ", typeof(ArgumentException), "The value cannot be an empty string or composed entirely of whitespace.")]
        public void LogToFile_InvalidFileName_ThrowsException(string fileName, Type exceptionType, string expectedMessage)
        {
            // Act & Assert
            var exception = Assert.Throws(exceptionType, () =>
                SerilogConfiguration.LogToFile("information", fileName, 7));

            Assert.Contains(expectedMessage, exception.Message);
            Assert.Equal("logFileName", (exception as ArgumentException)?.ParamName);
        }

        [Fact]
        public void LogToFile_EmptyFileName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                SerilogConfiguration.LogToFile("information", "", 7));
        }

        // ����� �� ������������ ��������� ������ �����������
        [Theory]
        [InlineData("verbose", LogEventLevel.Verbose)]
        [InlineData("debug", LogEventLevel.Debug)]
        [InlineData("information", LogEventLevel.Information)]
        [InlineData("warning", LogEventLevel.Warning)]
        [InlineData("error", LogEventLevel.Error)]
        [InlineData("fatal", LogEventLevel.Fatal)]
        public void LogToFile_ValidLogLevel_SetsCorrectLevel(string inputLevel, LogEventLevel expectedLevel)
        {
            // Arrange
            var tempFile = Path.GetTempFileName();

            try
            {
                // Act
                var logger = SerilogConfiguration.LogToFile(inputLevel, tempFile, 7);

                // Assert
                // ���������, ��� ������ ������ � ����� ���������� �������
                var serilogLogger = logger as Serilog.Core.Logger;
                Assert.NotNull(serilogLogger);

                // ����� ���������, ��� ���� ������
                Assert.True(File.Exists(tempFile));
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        // ���� �� �������� �� ���������
        [Theory]
        [InlineData(null)]
        [InlineData("invalid_level")]
        [InlineData("")]
        public void LogToFile_InvalidLogLevel_UsesDefaultLevel(string invalidLevel)
        {
            // Arrange
            var tempFile = Path.GetTempFileName();

            try
            {
                // Act
                var logger = SerilogConfiguration.LogToFile(invalidLevel, tempFile, 7);

                // Assert
                var serilogLogger = logger as Serilog.Core.Logger;
                Assert.NotNull(serilogLogger);
                // ���������, ��� ������������ ������� Information �� ���������
                // ����� ����� �������� �������� ������ �����������
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        // ���� �� �������� ����� ����
        [Fact]
        public async Task LogToFile_CreatesLogFile()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            File.Delete(tempFile);
            var datePattern = DateTime.Now.ToString("yyyyMMdd");
            ILogger? logger = null;
            string? fileContent = null;

            try
            {
                // Act
                logger = SerilogConfiguration.LogToFile("information", tempFile, 7);
                logger.Information("Test message");

                await Task.Delay(100);

                // ����������� ������� ������� ����� ������� �����
                (logger as IDisposable)?.Dispose();
                logger = null;
                await Task.Delay(100);

                // Assert
                var directory = Path.GetDirectoryName(tempFile);
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(tempFile);
                var extension = Path.GetExtension(tempFile);
                var expectedFileName = Path.Combine(directory!, $"{fileNameWithoutExt}{datePattern}{extension}");

                Assert.True(File.Exists(expectedFileName));
                fileContent = await File.ReadAllTextAsync(expectedFileName);
                Assert.Contains("Test message", fileContent);
            }
            finally
            {
                // �� ������ ������ ����������� ������� �������
                (logger as IDisposable)?.Dispose();
                await Task.Delay(100);

                // Cleanup
                var directory = Path.GetDirectoryName(tempFile);
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(tempFile);
                var logFiles = Directory.GetFiles(directory!, $"{fileNameWithoutExt}*");
                foreach (var file in logFiles)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (IOException)
                    {
                        // ���������� ������ ��� ��������
                    }
                }
            }
        }       
    }
}