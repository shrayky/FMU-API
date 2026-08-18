using FmuApiDomain.Configuration.Options;
using FmuApiDomain.ProductGroups.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.ProductGroups;

[Route("api/product-groups")]
[ApiController]
[ApiExplorerSettings(GroupName = "App configuration")]
public class ProductGroupsController(
    IGisMtProductMappingService mappingService,
    IGtinCatalogService gtinCatalogService) : ControllerBase
{
    [HttpGet("mapping")]
    public async Task<IActionResult> Mapping()
    {
        return Ok(await mappingService.List());
    }

    [HttpPut("mapping")]
    public async Task<IActionResult> SaveMapping([FromBody] GisMtProductMapping mapping)
    {
        if (mapping.AtolCode <= 0 || mapping.TrueApiGroupId <= 0)
            return BadRequest("Код Атол и код Честного знака должны быть больше нуля");

        var saved = await mappingService.Save(mapping);
        if (!saved)
            return BadRequest("Не удалось сохранить маппинг");

        return Ok(mapping);
    }

    [HttpDelete("mapping/{atolCode:int}")]
    public async Task<IActionResult> DeleteMapping(int atolCode)
    {
        var deleted = await mappingService.Delete(atolCode);
        if (!deleted)
            return BadRequest("Не удалось удалить маппинг");

        return Ok();
    }

    [HttpGet("gtin-catalog")]
    public async Task<IActionResult> GtinCatalog(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await gtinCatalogService.Search(search ?? string.Empty, page, pageSize);
        return Ok(result);
    }
}
