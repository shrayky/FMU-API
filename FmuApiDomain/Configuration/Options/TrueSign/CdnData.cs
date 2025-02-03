using CSharpFunctionalExtensions;
using JsonSerialShared.Json;
using System.Text.Json;

namespace FmuApiDomain.Configuration.Options.TrueSign
{
    public class CdnData
    {
        public List<TrueSignCdn> List { get; set; } = [];

        private readonly string fileName = @"cdn";

        public void LoadFromFile(string dataFolder)
        {
            string fullFileName = Path.Combine(dataFolder, $"{fileName}.json");

            if (!File.Exists(fullFileName))
                return;

            StreamReader file = new(fullFileName);

            string cdnText = file.ReadToEnd();
            file.Close();

            cdnText ??= "";

            if (cdnText == "")
                return;

            List<TrueSignCdn>? loadedCdns = [];

            try
            {
                loadedCdns = JsonSerializer.Deserialize<List<TrueSignCdn>>(cdnText);
            }
            catch
            {
                loadedCdns = [];
            }

            loadedCdns ??= [];

            List.AddRange(loadedCdns);
        }

        public async Task<Result<CdnData>> SaveAsync(string dataFolder)
        {
            string fullFileName = Path.Combine(dataFolder, $"{fileName}json");

            var saveData = await ListToStringJson();

            var success = await SaveTextToFileAsync(saveData, fullFileName);

            if (!success)
                return Result.Failure<CdnData>("Сохранение не удалось!");

            return Result.Success(this);
        }

        private async Task<string> ListToStringJson()
        {
            using MemoryStream stream = new();
            await JsonSerializer.SerializeAsync(stream, List, List.GetType(), JsonSerializeOptionsProvider.Default());

            stream.Position = 0;
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        private async Task<bool> SaveTextToFileAsync(string text, string fullFileName)
        {
            try
            {
                StreamWriter file = new(fullFileName, false);
                await file.WriteAsync(text);
                file.Close();
            }
            catch (Exception e)
            {
                return false;
            }

            return true;
        }

    }
}
