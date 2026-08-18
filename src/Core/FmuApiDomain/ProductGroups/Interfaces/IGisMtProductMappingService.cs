using FmuApiDomain.Configuration.Options;

namespace FmuApiDomain.ProductGroups.Interfaces;

public interface IGisMtProductMappingService
{
    Task<List<GisMtProductMapping>> List();

    Task<bool> Save(GisMtProductMapping mapping);

    Task<bool> Delete(int atolCode);
}
