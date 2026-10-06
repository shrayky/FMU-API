using FmuApiDomain.Database.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.System;

[Route("api/service/couchdb-import")]
[ApiController]
[ApiExplorerSettings(GroupName = "System")]
public class CouchDbImportController : ControllerBase
{
    private readonly ICouchDbToSqliteImport _couchDbImport;

    public CouchDbImportController(ICouchDbToSqliteImport couchDbImport)
    {
        _couchDbImport = couchDbImport;
    }

    /// <summary>
    /// Запускает перенос данных из CouchDB в SQLite и сразу отвечает, не дожидаясь конца копирования.
    /// </summary>
    [HttpPost]
    public IActionResult Start()
    {
        var result = _couchDbImport.Start();

        if (result.IsFailure)
        {
            if (result.Error == ICouchDbToSqliteImport.AlreadyRunningMessage)
                return Conflict(new { message = result.Error });

            return BadRequest(new { message = result.Error });
        }

        return Ok(_couchDbImport.State());
    }

    /// <summary>
    /// Возвращает текущее состояние переноса.
    /// </summary>
    [HttpGet]
    public IActionResult State()
    {
        return Ok(_couchDbImport.State());
    }
}
