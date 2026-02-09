using CSharpFunctionalExtensions;
using FmuApiApplication.Mark.Interfaces;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Interface;
using FmuApiDomain.Repositories;
using FmuApiDomain.State.Interfaces;
using FmuApiDomain.TrueApi.MarkData;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace FmuApiApplication.Documents
{
    public class BeginDocument : IFrontolDocumentService
    {
        private RequestDocument Document { get; set; }

        private Lazy<IDocumentRepository> TemporaryDocumentsService { get; set; }
        private Lazy<IMemoryCache> CacheService { get; set; }

        private IMarkFabric MarkFabric { get; set; }
        private IParametersService ParametersService { get; set; }
        private IApplicationState AppState { get; set; }
        
        private readonly Parameters _configuration;

        private BeginDocument(RequestDocument requestDocument, IServiceProvider provider)
        {
            Document = requestDocument;

            TemporaryDocumentsService = new Lazy<IDocumentRepository>(provider.GetRequiredService<IDocumentRepository>);
            CacheService = new Lazy<IMemoryCache>(provider.GetRequiredService<IMemoryCache>);
            
            MarkFabric = provider.GetRequiredService<IMarkFabric>();

            ParametersService = provider.GetRequiredService<IParametersService>();
            AppState = provider.GetRequiredService<IApplicationState>();
            _configuration = ParametersService.Current();
        }

        private static BeginDocument CreateObject(RequestDocument requestDocument, IServiceProvider provider) 
            => new(requestDocument, provider);
        
        public static IFrontolDocumentService Create(RequestDocument requestDocument, IServiceProvider provider) 
            => CreateObject(requestDocument, provider);
        

        public async Task<Result<FmuAnswer>> ActionAsync()
        {
            SendDocumentToAlcoUnitAsync();
            
            return await BeginDocumentAsync();
        }

        private async Task<Result<FmuAnswer>> BeginDocumentAsync()
        {
            FmuAnswer checkResult = new();

            foreach (var position in Document.Positions)
            {
                if (position.Marking_codes.Count == 0)
                    continue;

                foreach (var markInBase64 in position.Marking_codes)
                {
                    var mark = await MarkFabric.Create(position, markInBase64);

                    var trueApiCisData = await mark.TrueApiData();

                    if (trueApiCisData.Codes.Count == 0)
                        continue;

                    var markData = trueApiCisData.Codes[0];

                    if (!markData.GroupIds.Contains(TrueApiGroup.Tobaco))
                        continue;
                    
                    var minPrice = _configuration.MinimalPrices.Tabaco > markData.Smp ? _configuration.MinimalPrices.Tabaco : markData.Smp;

                    if (minPrice > position.Total_price * 100)
                    {
                        checkResult.Code = 3;
                        checkResult.Error += $"\r\n {position.Text} цена ниже минимальной розничной!";
                        checkResult.Marking_codes.Add(markInBase64);
                    }

                    if (markData.Mrp < position.Total_price * 100)
                    {
                        checkResult.Code = 3;
                        checkResult.Error += $"\r\n {position.Text} цена выше максимальной розничной!";
                        checkResult.Marking_codes.Add(markInBase64);
                    }

                }
            }

            if (_configuration.Database.ConfigurationIsEnabled && AppState.CouchDbOnline())
                await TemporaryDocumentsService.Value.Add(Document);
            else
                CacheService.Value.Set($"cashDoc_{Document.Uid}", Document, TimeSpan.FromMinutes(5));

            return Result.Success(checkResult);
        }

        private void SendDocumentToAlcoUnitAsync()
        {
            var auDoc = Document;

            if (_configuration.FrontolAlcoUnit.NetAdres == string.Empty)
                return;

            var positionsForDelete = new List<Position>();

            foreach (var pos in auDoc.Positions)
            {
                if (pos.Stamps.Count > 0)
                    continue;

                if (pos.Marking_codes.Count == 0)
                    continue;

                positionsForDelete.Add(pos);
            }

            foreach (var pos in positionsForDelete)
                auDoc.Positions.Remove(pos);
        }

    }
}
