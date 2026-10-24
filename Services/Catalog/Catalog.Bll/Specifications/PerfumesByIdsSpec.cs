using Ardalis.Specification;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Specifications;

public class PerfumesByIdsSpec : Specification<Perfume>
{
    public PerfumesByIdsSpec(IReadOnlyCollection<int> ids)
    {
        Query.Where(p => ids.Contains(p.Id) && !p.IsDeleted);
        Query.Include(p => p.Brand);
        Query.OrderBy(p => p.Id);
        Query.AsNoTracking();
    }
}