IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Brands] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Country] nvarchar(60) NULL,
    [FoundedYear] smallint NULL,
    CONSTRAINT [PK_Brands] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Brands_Year] CHECK ([FoundedYear] BETWEEN 1700 AND 2100)
);

CREATE TABLE [Notes] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(60) NOT NULL,
    [Family] nvarchar(20) NOT NULL,
    CONSTRAINT [PK_Notes] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Notes_Family] CHECK ([Family] IN (N'Citrus', N'Fruity', N'Gourmand', N'Spicy', N'Floral', N'Woody', N'Amber', N'Musk', N'Aquatic', N'Green', N'Smoky'))
);

CREATE TABLE [Perfumes] (
    [Id] int NOT NULL IDENTITY,
    [BrandId] int NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Sku] nvarchar(50) NOT NULL,
    [Gender] nvarchar(10) NOT NULL,
    [Concentration] nvarchar(10) NOT NULL,
    [Season] nvarchar(20) NOT NULL DEFAULT N'AllYear',
    [VolumeMl] smallint NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [CreatedBy] nvarchar(100) NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] nvarchar(100) NULL,
    [IsDeleted] bit NOT NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Perfumes] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Perfumes_Concentration] CHECK ([Concentration] IN (N'Parfum', N'EDP', N'EDT', N'EDC')),
    CONSTRAINT [CK_Perfumes_Gender] CHECK ([Gender] IN (N'Women', N'Men', N'Unisex')),
    CONSTRAINT [CK_Perfumes_Price] CHECK ([Price] > 0),
    CONSTRAINT [CK_Perfumes_Season] CHECK ([Season] IN (N'Spring', N'Summer', N'Autumn', N'Winter', N'AllYear')),
    CONSTRAINT [CK_Perfumes_Volume] CHECK ([VolumeMl] > 0),
    CONSTRAINT [FK_Perfumes_Brands_BrandId] FOREIGN KEY ([BrandId]) REFERENCES [Brands] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [PerfumeDetails] (
    [PerfumeId] int NOT NULL,
    [Description] nvarchar(2000) NOT NULL,
    [Perfumer] nvarchar(100) NULL,
    [LaunchYear] smallint NULL,
    [Longevity] tinyint NULL,
    CONSTRAINT [PK_PerfumeDetails] PRIMARY KEY ([PerfumeId]),
    CONSTRAINT [CK_PerfumeDetails_Longevity] CHECK ([Longevity] BETWEEN 1 AND 5),
    CONSTRAINT [CK_PerfumeDetails_Year] CHECK ([LaunchYear] BETWEEN 1800 AND 2100),
    CONSTRAINT [FK_PerfumeDetails_Perfumes_PerfumeId] FOREIGN KEY ([PerfumeId]) REFERENCES [Perfumes] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [PerfumeImages] (
    [Id] int NOT NULL IDENTITY,
    [PerfumeId] int NOT NULL,
    [Url] nvarchar(500) NOT NULL,
    [SortOrder] int NOT NULL,
    [IsMain] bit NOT NULL,
    CONSTRAINT [PK_PerfumeImages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PerfumeImages_Perfumes_PerfumeId] FOREIGN KEY ([PerfumeId]) REFERENCES [Perfumes] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [PerfumeNotes] (
    [PerfumeId] int NOT NULL,
    [NoteId] int NOT NULL,
    [Layer] nvarchar(10) NOT NULL,
    [AddedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_PerfumeNotes] PRIMARY KEY ([PerfumeId], [NoteId]),
    CONSTRAINT [CK_PerfumeNotes_Layer] CHECK ([Layer] IN (N'Top', N'Heart', N'Base')),
    CONSTRAINT [FK_PerfumeNotes_Notes_NoteId] FOREIGN KEY ([NoteId]) REFERENCES [Notes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PerfumeNotes_Perfumes_PerfumeId] FOREIGN KEY ([PerfumeId]) REFERENCES [Perfumes] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [UQ_Brands_Name] ON [Brands] ([Name]);

CREATE INDEX [IX_Notes_Family] ON [Notes] ([Family]) INCLUDE ([Name]);

CREATE UNIQUE INDEX [UQ_Notes_Name] ON [Notes] ([Name]);

CREATE INDEX [IX_PerfumeImages_Perfume_Sort] ON [PerfumeImages] ([PerfumeId], [SortOrder]);

CREATE UNIQUE INDEX [UX_PerfumeImages_OneMain] ON [PerfumeImages] ([PerfumeId]) WHERE [IsMain] = 1;

CREATE INDEX [IX_PerfumeNotes_NoteId] ON [PerfumeNotes] ([NoteId], [PerfumeId]) INCLUDE ([Layer]);

CREATE INDEX [IX_Perfumes_BrandId] ON [Perfumes] ([BrandId]) INCLUDE ([Name], [Price]) WHERE [IsDeleted] = 0;

CREATE INDEX [IX_Perfumes_Gender_Price] ON [Perfumes] ([Gender], [Price]) WHERE [IsActive] = 1 AND [IsDeleted] = 0;

CREATE INDEX [IX_Perfumes_Name] ON [Perfumes] ([Name]);

CREATE INDEX [IX_Perfumes_Season] ON [Perfumes] ([Season]) INCLUDE ([Name], [Price], [BrandId]) WHERE [IsActive] = 1 AND [IsDeleted] = 0;

CREATE UNIQUE INDEX [UQ_Perfumes_Brand_Name_Volume] ON [Perfumes] ([BrandId], [Name], [VolumeMl]);

CREATE UNIQUE INDEX [UQ_Perfumes_Sku] ON [Perfumes] ([Sku]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261024202342_InitialCreate', N'9.0.21');

COMMIT;
GO

