using Ardalis.Specification;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Specifications;

public class PerfumesByBrandSpec : Specification<Perfume>
{
    public PerfumesByBrandSpec(int brandId)
    {
        Query.Where(p => p.BrandId == brandId && !p.IsDeleted);
        Query.AsNoTracking();
    }
}