using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.Logging;

namespace CouchDb.Services;

public class DataBaseMaintenanceService
{
     private readonly ILogger<DataBaseMaintenanceService> _logger;
     private readonly IApplicationState _appState;
     private readonly CouchDbContext _dbContext;

     public DataBaseMaintenanceService(ILogger<DataBaseMaintenanceService> logger, IApplicationState appState, CouchDbContext dbContext)
     {
          _logger = logger;
          _appState = appState;
          _dbContext = dbContext;
     }

     public async Task<bool> CompactDatabase()
     {
          if (!_appState.CouchDbOnline())
               return false;

          try
          {
               await _dbContext.Marks.CompactAsync();
               await _dbContext.Documents.CompactAsync();
               await _dbContext.MarkCheckingStatistic.CompactAsync();
          }
          catch (Exception e)
          {
               _logger.LogError("Ошибка сжатия БД: {err}", e.Message);
               return false;
          }

          return true;
     }
}