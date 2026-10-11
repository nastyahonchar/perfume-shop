SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
GO
USE PerfumeCatalogDb;
GO

INSERT INTO dbo.Brands (Name, Country, FoundedYear)
SELECT v.Name, v.Country, v.FoundedYear
FROM (VALUES
    (N'Maison Aurelle', N'France', 1952),
    (N'Nordic Bloom',   N'Sweden', 2009),
    (N'Velvet Atelier', N'Italy',  1987),
    (N'Casa Ambra',     N'Spain',  1999),
    (N'Oud & Co',       N'UAE',    2014)
) AS v (Name, Country, FoundedYear)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Brands b WHERE b.Name = v.Name);

INSERT INTO dbo.Notes (Name, Family)
SELECT v.Name, v.Family
FROM (VALUES
    (N'Bergamot', N'Citrus'), (N'Lemon', N'Citrus'), (N'Grapefruit', N'Citrus'),
    (N'Cherry', N'Fruity'), (N'Apple', N'Fruity'), (N'Fig', N'Fruity'),
    (N'Pumpkin', N'Gourmand'), (N'Vanilla', N'Gourmand'), (N'Caramel', N'Gourmand'), (N'Coffee', N'Gourmand'),
    (N'Cinnamon', N'Spicy'), (N'Clove', N'Spicy'), (N'Cardamom', N'Spicy'), (N'Pink Pepper', N'Spicy'),
    (N'Rose', N'Floral'), (N'Iris', N'Floral'), (N'Jasmine', N'Floral'), (N'Cherry Blossom', N'Floral'),
    (N'Cedar', N'Woody'), (N'Sandalwood', N'Woody'), (N'Patchouli', N'Woody'), (N'Oud', N'Woody'), (N'Vetiver', N'Woody'),
    (N'Amber', N'Amber'), (N'Musk', N'Musk'), (N'Sea Salt', N'Aquatic'), (N'Mint', N'Green'), (N'Smoke', N'Smoky')
) AS v (Name, Family)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Notes n WHERE n.Name = v.Name);
GO

-- Id парфумів фіксовані: на них посилаються бази Замовлень і Відгуків
SET IDENTITY_INSERT dbo.Perfumes ON;
INSERT INTO dbo.Perfumes (Id, BrandId, Name, Sku, Gender, Concentration, Season, VolumeMl, Price, CreatedBy)
SELECT v.Id, b.Id, v.Name, v.Sku, v.Gender, v.Concentration, v.Season, v.VolumeMl, v.Price, N'seed'
FROM (VALUES
    (1,  N'Maison Aurelle', N'Pumpkin Velours', N'MA-PV-50',  N'Women',  N'EDP',    N'Autumn',  50,  2890.00),
    (2,  N'Maison Aurelle', N'Citrus Riviera',  N'MA-CR-100', N'Unisex', N'EDT',    N'Summer',  100, 2450.00),
    (3,  N'Nordic Bloom',   N'Cherry Meadow',   N'NB-CM-50',  N'Women',  N'EDT',    N'Spring',  50,  2190.00),
    (4,  N'Nordic Bloom',   N'Cedar Snow',      N'NB-CS-100', N'Men',    N'EDP',    N'Winter',  100, 3250.00),
    (5,  N'Velvet Atelier', N'Spiced Orchard',  N'VA-SO-50',  N'Unisex', N'EDP',    N'Autumn',  50,  3100.00),
    (6,  N'Velvet Atelier', N'Black Iris',      N'VA-BI-50',  N'Unisex', N'Parfum', N'AllYear', 50,  4200.00),
    (7,  N'Casa Ambra',     N'Amber Harvest',   N'CA-AH-100', N'Men',    N'EDP',    N'Autumn',  100, 3490.00),
    (8,  N'Casa Ambra',     N'Sea Salt Noon',   N'CA-SN-50',  N'Unisex', N'EDT',    N'Summer',  50,  2290.00),
    (9,  N'Oud & Co',       N'Smoked Fig',      N'OC-SF-50',  N'Unisex', N'Parfum', N'Autumn',  50,  4590.00),
    (10, N'Oud & Co',       N'Snow Vanilla',    N'OC-SV-100', N'Women',  N'EDP',    N'Winter',  100, 3790.00)
) AS v (Id, Brand, Name, Sku, Gender, Concentration, Season, VolumeMl, Price)
JOIN dbo.Brands b ON b.Name = v.Brand
WHERE NOT EXISTS (SELECT 1 FROM dbo.Perfumes p WHERE p.Id = v.Id);
SET IDENTITY_INSERT dbo.Perfumes OFF;
GO

INSERT INTO dbo.PerfumeDetails (PerfumeId, Description, Perfumer, LaunchYear, Longevity)
SELECT v.PerfumeId, v.Description, v.Perfumer, v.LaunchYear, v.Longevity
FROM (VALUES
    (1,  N'Осінній гурманський аромат: запечений гарбуз, кориця та тепла ваніль.',       N'A. Laurent', 2024, 4),
    (2,  N'Свіжий цитрусовий аромат для спекотних днів: бергамот, лимон і грейпфрут.',    NULL,          2021, 2),
    (3,  N'Весняний квітково-фруктовий аромат із вишневим цвітом.',                        NULL,          2022, 3),
    (4,  N'Холодний деревний аромат: кедр, ірис і кардамон.',                              N'M. Berg',    2020, 4),
    (5,  N'Яблучний сад із прянощами: яблуко, кориця, гвоздика та карамель.',              N'G. Rossi',   2023, 4),
    (6,  N'Глибокий квітково-деревний аромат на весь рік: ірис, пачулі, уд.',              N'G. Rossi',   2018, 5),
    (7,  N'Теплий бурштиново-прянний аромат: кардамон, кава, бурштин і кедр.',             NULL,          2023, 4),
    (8,  N'Морський аромат із сіллю та жасмином.',                                         NULL,          2022, 2),
    (9,  N'Димний інжир на основі уду для вечорів.',                                       N'Y. Haddad',  2025, 5),
    (10, N'Зимова ваніль із м''ятною свіжістю та сандалом.',                               N'Y. Haddad',  2024, 4)
) AS v (PerfumeId, Description, Perfumer, LaunchYear, Longevity)
WHERE NOT EXISTS (SELECT 1 FROM dbo.PerfumeDetails d WHERE d.PerfumeId = v.PerfumeId);

INSERT INTO dbo.PerfumeImages (PerfumeId, Url, SortOrder, IsMain)
SELECT p.Id, v.Url, v.SortOrder, v.IsMain
FROM dbo.Perfumes p
CROSS APPLY (VALUES
    (CONCAT(N'/images/perfumes/', p.Id, N'-main.jpg'), 0, 1),
    (CONCAT(N'/images/perfumes/', p.Id, N'-box.jpg'),  1, 0)
) AS v (Url, SortOrder, IsMain)
WHERE NOT EXISTS (SELECT 1 FROM dbo.PerfumeImages i WHERE i.PerfumeId = p.Id AND i.Url = v.Url);
GO

DECLARE @PN TABLE (Sku NVARCHAR(50), Note NVARCHAR(60), Layer NVARCHAR(10));
INSERT INTO @PN (Sku, Note, Layer) VALUES
 (N'MA-PV-50', N'Pink Pepper', N'Top'),  (N'MA-PV-50', N'Apple', N'Top'),
 (N'MA-PV-50', N'Pumpkin', N'Heart'),    (N'MA-PV-50', N'Cinnamon', N'Heart'),
 (N'MA-PV-50', N'Vanilla', N'Base'),     (N'MA-PV-50', N'Amber', N'Base'),      (N'MA-PV-50', N'Sandalwood', N'Base'),

 (N'MA-CR-100', N'Bergamot', N'Top'),    (N'MA-CR-100', N'Lemon', N'Top'),      (N'MA-CR-100', N'Grapefruit', N'Top'),
 (N'MA-CR-100', N'Jasmine', N'Heart'),   (N'MA-CR-100', N'Mint', N'Heart'),
 (N'MA-CR-100', N'Musk', N'Base'),       (N'MA-CR-100', N'Vetiver', N'Base'),

 (N'NB-CM-50', N'Cherry', N'Top'),       (N'NB-CM-50', N'Bergamot', N'Top'),
 (N'NB-CM-50', N'Cherry Blossom', N'Heart'), (N'NB-CM-50', N'Rose', N'Heart'),
 (N'NB-CM-50', N'Musk', N'Base'),        (N'NB-CM-50', N'Sandalwood', N'Base'),

 (N'NB-CS-100', N'Cardamom', N'Top'),    (N'NB-CS-100', N'Mint', N'Top'),
 (N'NB-CS-100', N'Cedar', N'Heart'),     (N'NB-CS-100', N'Iris', N'Heart'),
 (N'NB-CS-100', N'Vetiver', N'Base'),    (N'NB-CS-100', N'Amber', N'Base'),

 (N'VA-SO-50', N'Apple', N'Top'),        (N'VA-SO-50', N'Bergamot', N'Top'),
 (N'VA-SO-50', N'Cinnamon', N'Heart'),   (N'VA-SO-50', N'Clove', N'Heart'),
 (N'VA-SO-50', N'Caramel', N'Base'),     (N'VA-SO-50', N'Patchouli', N'Base'),

 (N'VA-BI-50', N'Pink Pepper', N'Top'),
 (N'VA-BI-50', N'Iris', N'Heart'),       (N'VA-BI-50', N'Rose', N'Heart'),
 (N'VA-BI-50', N'Patchouli', N'Base'),   (N'VA-BI-50', N'Oud', N'Base'),        (N'VA-BI-50', N'Musk', N'Base'),

 (N'CA-AH-100', N'Cardamom', N'Top'),    (N'CA-AH-100', N'Apple', N'Top'),
 (N'CA-AH-100', N'Cinnamon', N'Heart'),  (N'CA-AH-100', N'Coffee', N'Heart'),
 (N'CA-AH-100', N'Amber', N'Base'),      (N'CA-AH-100', N'Cedar', N'Base'),     (N'CA-AH-100', N'Vanilla', N'Base'),

 (N'CA-SN-50', N'Grapefruit', N'Top'),   (N'CA-SN-50', N'Lemon', N'Top'),
 (N'CA-SN-50', N'Sea Salt', N'Heart'),   (N'CA-SN-50', N'Jasmine', N'Heart'),
 (N'CA-SN-50', N'Musk', N'Base'),        (N'CA-SN-50', N'Vetiver', N'Base'),

 (N'OC-SF-50', N'Fig', N'Top'),          (N'OC-SF-50', N'Cardamom', N'Top'),
 (N'OC-SF-50', N'Smoke', N'Heart'),      (N'OC-SF-50', N'Cinnamon', N'Heart'),
 (N'OC-SF-50', N'Oud', N'Base'),         (N'OC-SF-50', N'Patchouli', N'Base'),  (N'OC-SF-50', N'Amber', N'Base'),

 (N'OC-SV-100', N'Mint', N'Top'),        (N'OC-SV-100', N'Bergamot', N'Top'),
 (N'OC-SV-100', N'Jasmine', N'Heart'),   (N'OC-SV-100', N'Rose', N'Heart'),
 (N'OC-SV-100', N'Vanilla', N'Base'),    (N'OC-SV-100', N'Sandalwood', N'Base'), (N'OC-SV-100', N'Musk', N'Base');

INSERT INTO dbo.PerfumeNotes (PerfumeId, NoteId, Layer)
SELECT p.Id, n.Id, v.Layer
FROM @PN v
JOIN dbo.Perfumes p ON p.Sku = v.Sku
JOIN dbo.Notes n    ON n.Name = v.Note
WHERE NOT EXISTS (SELECT 1 FROM dbo.PerfumeNotes pn WHERE pn.PerfumeId = p.Id AND pn.NoteId = n.Id);
GO