using Ardalis.Specification;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Specifications;

public class BrandByNameSpec : Specification<Brand>
{
    public BrandByNameSpec(string name)
    {
        Query.Where(b => b.Name == name);
    }
}