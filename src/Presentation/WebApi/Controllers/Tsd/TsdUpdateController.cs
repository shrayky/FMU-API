using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.SoftwareUpdate;
using GitHubReleaseUpdate.Services;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Tsd;

/// Отдаёт пакет FmuApiTSD из каталога ноды и забирает его из сохранённого релиза.
[Route("api/tsd/update")]
[ApiController]
[ApiExplorerSettings(GroupName = "TSD")]
public class TsdUpdateController : ControllerBase
{
    private readonly ClientPackageCache _cache;
    private readonly IParametersService _parametersService;

    public TsdUpdateController(ClientPackageCache cache, IParametersService parametersService)
    {
        _cache = cache;
        _parametersService = parametersService;
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var address = _parametersService.Current().TsdAppUpdate.ReleaseAddress();
        var result = await _cache.Refresh(ClientApplications.Tsd, address, cancellationToken);
        if (result.IsFailure)
            return BadRequest(result.Error);

        if (result.Value.Downloaded == 0 && result.Value.Errors.Count > 0)
            return BadRequest(string.Join("; ", result.Value.Errors));

        return Ok(new { downloaded = result.Value.Downloaded, errors = result.Value.Errors });
    }

    [HttpGet("stored")]
    public IActionResult Stored()
    {
        var packages = ClientPackageCatalog.Stored(ClientApplications.Tsd);
        return Ok(packages.Select(file => new
        {
            versionName = file.Package.VersionName,
            versionCode = file.Package.VersionCode,
            abi = file.Package.Abi,
            fileName = file.Package.FileName,
        }));
    }

    [HttpGet]
    public IActionResult Metadata([FromQuery] string? abi)
    {
        if (string.IsNullOrWhiteSpace(abi))
            return BadRequest("Не указан abi");

        var package = ClientPackageCatalog.Latest(ClientApplications.Tsd, abi);
        if (package is null)
            return NotFound();

        return Ok(new
        {
            versionName = package.Package.VersionName,
            versionCode = package.Package.VersionCode,
            sha256 = package.Sha256,
            url = "/api/tsd/update/apk?abi=" + Uri.EscapeDataString(package.Package.Abi)
        });
    }

    [HttpGet("apk")]
    public IActionResult Apk([FromQuery] string? abi)
    {
        if (string.IsNullOrWhiteSpace(abi))
            return BadRequest("Не указан abi");

        var package = ClientPackageCatalog.Latest(ClientApplications.Tsd, abi);
        if (package is null)
            return NotFound();

        return PhysicalFile(
            package.FilePath,
            "application/vnd.android.package-archive",
            package.Package.FileName);
    }
}
