SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
GO
IF DB_ID(N'PerfumeCatalogDb') IS NULL CREATE DATABASE PerfumeCatalogDb;
GO
USE PerfumeCatalogDb;
GO
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'dbo.Brands', N'U') IS NULL
CREATE TABLE dbo.Brands (
    Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Brands PRIMARY KEY,
    Name        NVARCHAR(100) NOT NULL CONSTRAINT UQ_Brands_Name UNIQUE,
    Country     NVARCHAR(60)  NULL,
    FoundedYear SMALLINT      NULL CONSTRAINT CK_Brands_Year CHECK (FoundedYear BETWEEN 1700 AND 2100)
);
GO

IF OBJECT_ID(N'dbo.Perfumes', N'U') IS NULL
CREATE TABLE dbo.Perfumes (
    Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Perfumes PRIMARY KEY,
    BrandId       INT NOT NULL CONSTRAINT FK_Perfumes_Brands REFERENCES dbo.Brands(Id),
    Name          NVARCHAR(200) NOT NULL,
    Sku           NVARCHAR(50)  NOT NULL CONSTRAINT UQ_Perfumes_Sku UNIQUE,
    Gender        NVARCHAR(10)  NOT NULL CONSTRAINT CK_Perfumes_Gender CHECK (Gender IN (N'Women', N'Men', N'Unisex')),
    Concentration NVARCHAR(10)  NOT NULL CONSTRAINT CK_Perfumes_Concentration CHECK (Concentration IN (N'Parfum', N'EDP', N'EDT', N'EDC')),
    Season        NVARCHAR(20)  NOT NULL CONSTRAINT DF_Perfumes_Season DEFAULT N'AllYear'
        CONSTRAINT CK_Perfumes_Season CHECK (Season IN (N'Spring', N'Summer', N'Autumn', N'Winter', N'AllYear')),
    VolumeMl      SMALLINT      NOT NULL CONSTRAINT CK_Perfumes_Volume CHECK (VolumeMl > 0),
    Price         DECIMAL(18,2) NOT NULL CONSTRAINT CK_Perfumes_Price CHECK (Price > 0),
    IsActive      BIT           NOT NULL CONSTRAINT DF_Perfumes_IsActive DEFAULT 1,
    CreatedAt     DATETIME2     NOT NULL CONSTRAINT DF_Perfumes_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy     NVARCHAR(100) NULL,
    UpdatedAt     DATETIME2     NULL,
    UpdatedBy     NVARCHAR(100) NULL,
    IsDeleted     BIT           NOT NULL CONSTRAINT DF_Perfumes_IsDeleted DEFAULT 0,
    RowVersion    ROWVERSION    NOT NULL,
    CONSTRAINT UQ_Perfumes_Brand_Name_Volume UNIQUE (BrandId, Name, VolumeMl)
);
GO

IF OBJECT_ID(N'dbo.PerfumeDetails', N'U') IS NULL
CREATE TABLE dbo.PerfumeDetails (
    PerfumeId   INT NOT NULL CONSTRAINT PK_PerfumeDetails PRIMARY KEY
        CONSTRAINT FK_PerfumeDetails_Perfumes REFERENCES dbo.Perfumes(Id) ON DELETE CASCADE,
    Description NVARCHAR(2000) NOT NULL,
    Perfumer    NVARCHAR(100)  NULL,
    LaunchYear  SMALLINT       NULL CONSTRAINT CK_PerfumeDetails_Year CHECK (LaunchYear BETWEEN 1800 AND 2100),
    Longevity   TINYINT        NULL CONSTRAINT CK_PerfumeDetails_Longevity CHECK (Longevity BETWEEN 1 AND 5)
);
GO

IF OBJECT_ID(N'dbo.PerfumeImages', N'U') IS NULL
CREATE TABLE dbo.PerfumeImages (
    Id        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PerfumeImages PRIMARY KEY,
    PerfumeId INT NOT NULL CONSTRAINT FK_PerfumeImages_Perfumes REFERENCES dbo.Perfumes(Id) ON DELETE CASCADE,
    Url       NVARCHAR(500) NOT NULL,
    SortOrder INT NOT NULL CONSTRAINT DF_PerfumeImages_Sort DEFAULT 0,
    IsMain    BIT NOT NULL CONSTRAINT DF_PerfumeImages_IsMain DEFAULT 0
);
GO

IF OBJECT_ID(N'dbo.Notes', N'U') IS NULL
CREATE TABLE dbo.Notes (
    Id     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Notes PRIMARY KEY,
    Name   NVARCHAR(60) NOT NULL CONSTRAINT UQ_Notes_Name UNIQUE,
    Family NVARCHAR(20) NOT NULL CONSTRAINT CK_Notes_Family CHECK (Family IN
        (N'Citrus', N'Fruity', N'Gourmand', N'Spicy', N'Floral', N'Woody', N'Amber', N'Musk', N'Aquatic', N'Green', N'Smoky'))
);
GO

-- зв'язок M:N між парфумами й нотами; Layer і AddedAt - власні поля зв'язку
IF OBJECT_ID(N'dbo.PerfumeNotes', N'U') IS NULL
CREATE TABLE dbo.PerfumeNotes (
    PerfumeId INT NOT NULL CONSTRAINT FK_PerfumeNotes_Perfumes REFERENCES dbo.Perfumes(Id) ON DELETE CASCADE,
    NoteId    INT NOT NULL CONSTRAINT FK_PerfumeNotes_Notes    REFERENCES dbo.Notes(Id),
    Layer     NVARCHAR(10) NOT NULL CONSTRAINT CK_PerfumeNotes_Layer CHECK (Layer IN (N'Top', N'Heart', N'Base')),
    AddedAt   DATETIME2 NOT NULL CONSTRAINT DF_PerfumeNotes_AddedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_PerfumeNotes PRIMARY KEY (PerfumeId, NoteId)
);
GO

-- Індекси. Біля кожного вказано запит, під який він створений.

-- усі парфуми бренду
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Perfumes_BrandId' AND object_id = OBJECT_ID(N'dbo.Perfumes'))
    CREATE INDEX IX_Perfumes_BrandId ON dbo.Perfumes (BrandId) INCLUDE (Name, Price) WHERE IsDeleted = 0;

-- сезонна вітрина: активні парфуми певного сезону
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Perfumes_Season' AND object_id = OBJECT_ID(N'dbo.Perfumes'))
    CREATE INDEX IX_Perfumes_Season ON dbo.Perfumes (Season) INCLUDE (Name, Price, BrandId) WHERE IsActive = 1 AND IsDeleted = 0;

-- пошук і сортування за назвою
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Perfumes_Name' AND object_id = OBJECT_ID(N'dbo.Perfumes'))
    CREATE INDEX IX_Perfumes_Name ON dbo.Perfumes (Name);

-- фільтр за статтю й ціною
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Perfumes_Gender_Price' AND object_id = OBJECT_ID(N'dbo.Perfumes'))
    CREATE INDEX IX_Perfumes_Gender_Price ON dbo.Perfumes (Gender, Price) WHERE IsActive = 1 AND IsDeleted = 0;

-- фільтр за нотою: які парфуми містять ноту X
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PerfumeNotes_NoteId' AND object_id = OBJECT_ID(N'dbo.PerfumeNotes'))
    CREATE INDEX IX_PerfumeNotes_NoteId ON dbo.PerfumeNotes (NoteId, PerfumeId) INCLUDE (Layer);

-- список нот за родиною для фільтра
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Notes_Family' AND object_id = OBJECT_ID(N'dbo.Notes'))
    CREATE INDEX IX_Notes_Family ON dbo.Notes (Family) INCLUDE (Name);

-- фото товару у правильному порядку
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PerfumeImages_Perfume_Sort' AND object_id = OBJECT_ID(N'dbo.PerfumeImages'))
    CREATE INDEX IX_PerfumeImages_Perfume_Sort ON dbo.PerfumeImages (PerfumeId, SortOrder);

-- не більше одного головного фото на товар
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_PerfumeImages_OneMain' AND object_id = OBJECT_ID(N'dbo.PerfumeImages'))
    CREATE UNIQUE INDEX UX_PerfumeImages_OneMain ON dbo.PerfumeImages (PerfumeId) WHERE IsMain = 1;
GO

-- представлення для читання: картка парфуму з нотами піраміди в одному рядку
CREATE OR ALTER VIEW dbo.vw_PerfumeCard
AS
SELECT p.Id, b.Name AS Brand, p.Name, p.Sku, p.Gender, p.Concentration, p.Season, p.VolumeMl, p.Price,
       (SELECT STRING_AGG(n.Name, N', ') FROM dbo.PerfumeNotes pn JOIN dbo.Notes n ON n.Id = pn.NoteId
        WHERE pn.PerfumeId = p.Id AND pn.Layer = N'Top')   AS TopNotes,
       (SELECT STRING_AGG(n.Name, N', ') FROM dbo.PerfumeNotes pn JOIN dbo.Notes n ON n.Id = pn.NoteId
        WHERE pn.PerfumeId = p.Id AND pn.Layer = N'Heart') AS HeartNotes,
       (SELECT STRING_AGG(n.Name, N', ') FROM dbo.PerfumeNotes pn JOIN dbo.Notes n ON n.Id = pn.NoteId
        WHERE pn.PerfumeId = p.Id AND pn.Layer = N'Base')  AS BaseNotes
FROM dbo.Perfumes p
JOIN dbo.Brands b ON b.Id = p.BrandId
WHERE p.IsActive = 1 AND p.IsDeleted = 0;
GO