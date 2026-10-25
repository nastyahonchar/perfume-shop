using Catalog.Domain.Constants;
using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Seeding;

public static class DbInitializer
{
    private sealed record PerfumeSeed(
        int Id, int BrandId, string Name, string Sku, string Gender, string Concentration, string Season,
        short VolumeMl, decimal Price, string Description, string? Perfumer, short LaunchYear, byte Longevity,
        (string Note, string Layer)[] Notes);

    private static readonly PerfumeSeed[] Data =
    {
        new(1, 1, "Pumpkin Velours", "MA-PV-50", Genders.Women, Concentrations.EDP, Seasons.Autumn, 50, 2890.00m,
            "Осінній гурманський аромат: запечений гарбуз, кориця та тепла ваніль.", "A. Laurent", 2024, 4,
            new[] { ("Pink Pepper", NoteLayers.Top), ("Apple", NoteLayers.Top),
                    ("Pumpkin", NoteLayers.Heart), ("Cinnamon", NoteLayers.Heart),
                    ("Vanilla", NoteLayers.Base), ("Amber", NoteLayers.Base), ("Sandalwood", NoteLayers.Base) }),

        new(2, 1, "Citrus Riviera", "MA-CR-100", Genders.Unisex, Concentrations.EDT, Seasons.Summer, 100, 2450.00m,
            "Свіжий цитрусовий аромат для спекотних днів: бергамот, лимон і грейпфрут.", null, 2021, 2,
            new[] { ("Bergamot", NoteLayers.Top), ("Lemon", NoteLayers.Top), ("Grapefruit", NoteLayers.Top),
                    ("Jasmine", NoteLayers.Heart), ("Mint", NoteLayers.Heart),
                    ("Musk", NoteLayers.Base), ("Vetiver", NoteLayers.Base) }),

        new(3, 2, "Cherry Meadow", "NB-CM-50", Genders.Women, Concentrations.EDT, Seasons.Spring, 50, 2190.00m,
            "Весняний квітково-фруктовий аромат із вишневим цвітом.", null, 2022, 3,
            new[] { ("Cherry", NoteLayers.Top), ("Bergamot", NoteLayers.Top),
                    ("Cherry Blossom", NoteLayers.Heart), ("Rose", NoteLayers.Heart),
                    ("Musk", NoteLayers.Base), ("Sandalwood", NoteLayers.Base) }),

        new(4, 2, "Cedar Snow", "NB-CS-100", Genders.Men, Concentrations.EDP, Seasons.Winter, 100, 3250.00m,
            "Холодний деревний аромат: кедр, ірис і кардамон.", "M. Berg", 2020, 4,
            new[] { ("Cardamom", NoteLayers.Top), ("Mint", NoteLayers.Top),
                    ("Cedar", NoteLayers.Heart), ("Iris", NoteLayers.Heart),
                    ("Vetiver", NoteLayers.Base), ("Amber", NoteLayers.Base) }),

        new(5, 3, "Spiced Orchard", "VA-SO-50", Genders.Unisex, Concentrations.EDP, Seasons.Autumn, 50, 3100.00m,
            "Яблучний сад із прянощами: яблуко, кориця, гвоздика та карамель.", "G. Rossi", 2023, 4,
            new[] { ("Apple", NoteLayers.Top), ("Bergamot", NoteLayers.Top),
                    ("Cinnamon", NoteLayers.Heart), ("Clove", NoteLayers.Heart),
                    ("Caramel", NoteLayers.Base), ("Patchouli", NoteLayers.Base) }),

        new(6, 3, "Black Iris", "VA-BI-50", Genders.Unisex, Concentrations.Parfum, Seasons.AllYear, 50, 4200.00m,
            "Глибокий квітково-деревний аромат на весь рік: ірис, пачулі, уд.", "G. Rossi", 2018, 5,
            new[] { ("Pink Pepper", NoteLayers.Top),
                    ("Iris", NoteLayers.Heart), ("Rose", NoteLayers.Heart),
                    ("Patchouli", NoteLayers.Base), ("Oud", NoteLayers.Base), ("Musk", NoteLayers.Base) }),

        new(7, 4, "Amber Harvest", "CA-AH-100", Genders.Men, Concentrations.EDP, Seasons.Autumn, 100, 3490.00m,
            "Теплий бурштиново-прянний аромат: кардамон, кава, бурштин і кедр.", null, 2023, 4,
            new[] { ("Cardamom", NoteLayers.Top), ("Apple", NoteLayers.Top),
                    ("Cinnamon", NoteLayers.Heart), ("Coffee", NoteLayers.Heart),
                    ("Amber", NoteLayers.Base), ("Cedar", NoteLayers.Base), ("Vanilla", NoteLayers.Base) }),

        new(8, 4, "Sea Salt Noon", "CA-SN-50", Genders.Unisex, Concentrations.EDT, Seasons.Summer, 50, 2290.00m,
            "Морський аромат із сіллю та жасмином.", null, 2022, 2,
            new[] { ("Grapefruit", NoteLayers.Top), ("Lemon", NoteLayers.Top),
                    ("Sea Salt", NoteLayers.Heart), ("Jasmine", NoteLayers.Heart),
                    ("Musk", NoteLayers.Base), ("Vetiver", NoteLayers.Base) }),

        new(9, 5, "Smoked Fig", "OC-SF-50", Genders.Unisex, Concentrations.Parfum, Seasons.Autumn, 50, 4590.00m,
            "Димний інжир на основі уду для вечорів.", "Y. Haddad", 2025, 5,
            new[] { ("Fig", NoteLayers.Top), ("Cardamom", NoteLayers.Top),
                    ("Smoke", NoteLayers.Heart), ("Cinnamon", NoteLayers.Heart),
                    ("Oud", NoteLayers.Base), ("Patchouli", NoteLayers.Base), ("Amber", NoteLayers.Base) }),

        new(10, 5, "Snow Vanilla", "OC-SV-100", Genders.Women, Concentrations.EDP, Seasons.Winter, 100, 3790.00m,
            "Зимова ваніль із м'ятною свіжістю та сандалом.", "Y. Haddad", 2024, 4,
            new[] { ("Mint", NoteLayers.Top), ("Bergamot", NoteLayers.Top),
                    ("Jasmine", NoteLayers.Heart), ("Rose", NoteLayers.Heart),
                    ("Vanilla", NoteLayers.Base), ("Sandalwood", NoteLayers.Base), ("Musk", NoteLayers.Base) })
    };

    public static async Task SeedAsync(CatalogDbContext db, CancellationToken ct = default)
    {
        if (await db.Perfumes.AnyAsync(ct))
            return;

        var noteIds = await db.Notes.AsNoTracking().ToDictionaryAsync(n => n.Name, n => n.Id, ct);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT dbo.Perfumes ON", ct);

        foreach (var s in Data)
        {
            var perfume = new Perfume(
                s.BrandId, s.Name, s.Sku, s.Gender, s.Concentration, s.Season, s.VolumeMl, s.Price, "seed");

            db.Entry(perfume).Property(p => p.Id).CurrentValue = s.Id;

            perfume.SetDetail(s.Description, s.Perfumer, s.LaunchYear, s.Longevity);
            perfume.AddImage($"/images/perfumes/{s.Id}-main.jpg", isMain: true);
            perfume.AddImage($"/images/perfumes/{s.Id}-box.jpg", isMain: false);

            foreach (var (note, layer) in s.Notes)
                perfume.AddNote(noteIds[note], layer);

            db.Perfumes.Add(perfume);
        }

        await db.SaveChangesAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT dbo.Perfumes OFF", ct);
        await transaction.CommitAsync(ct);
    }
}