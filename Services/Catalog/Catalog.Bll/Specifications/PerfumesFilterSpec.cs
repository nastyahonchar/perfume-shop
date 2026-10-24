using Ardalis.Specification;
using Catalog.Bll.Queries;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Specifications;

public class PerfumesFilterSpec : Specification<Perfume>
{
    public PerfumesFilterSpec(PerfumeQuery q)
    {
        Query.Where(p => p.IsActive && !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.Trim();
            Query.Where(p => p.Name.Contains(term) || p.Brand.Name.Contains(term));
        }

        if (q.BrandId is not null)
        {
            var brandId = q.BrandId.Value;
            Query.Where(p => p.BrandId == brandId);
        }

        if (!string.IsNullOrWhiteSpace(q.Season))
        {
            var season = q.Season;
            Query.Where(p => p.Season == season);
        }

        if (!string.IsNullOrWhiteSpace(q.Gender))
        {
            var gender = q.Gender;
            Query.Where(p => p.Gender == gender);
        }

        if (q.MinPrice is not null)
        {
            var min = q.MinPrice.Value;
            Query.Where(p => p.Price >= min);
        }

        if (q.MaxPrice is not null)
        {
            var max = q.MaxPrice.Value;
            Query.Where(p => p.Price <= max);
        }

        var notes = (q.Notes ?? new List<string>())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var note in notes)
            Query.Where(p => p.PerfumeNotes.Any(pn => pn.Note.Name == note));

        Query.Include(p => p.Brand);

        var desc = string.Equals(q.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        if (string.Equals(q.SortBy, "price", StringComparison.OrdinalIgnoreCase))
        {
            if (desc) Query.OrderByDescending(p => p.Price).ThenBy(p => p.Id);
            else Query.OrderBy(p => p.Price).ThenBy(p => p.Id);
        }
        else
        {
            if (desc) Query.OrderByDescending(p => p.Name).ThenBy(p => p.Id);
            else Query.OrderBy(p => p.Name).ThenBy(p => p.Id);
        }

        var page = Math.Max(q.Page, 1);
        var pageSize = Math.Clamp(q.PageSize, 1, 100);
        Query.Skip((page - 1) * pageSize).Take(pageSize);

        Query.AsNoTracking();
    }
}