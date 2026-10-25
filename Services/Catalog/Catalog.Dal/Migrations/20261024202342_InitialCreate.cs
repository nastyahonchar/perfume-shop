using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Dal.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Brands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    FoundedYear = table.Column<short>(type: "smallint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brands", x => x.Id);
                    table.CheckConstraint("CK_Brands_Year", "[FoundedYear] BETWEEN 1700 AND 2100");
                });

            migrationBuilder.CreateTable(
                name: "Notes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Family = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notes", x => x.Id);
                    table.CheckConstraint("CK_Notes_Family", "[Family] IN (N'Citrus', N'Fruity', N'Gourmand', N'Spicy', N'Floral', N'Woody', N'Amber', N'Musk', N'Aquatic', N'Green', N'Smoky')");
                });

            migrationBuilder.CreateTable(
                name: "Perfumes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BrandId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Sku = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Concentration = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Season = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "AllYear"),
                    VolumeMl = table.Column<short>(type: "smallint", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perfumes", x => x.Id);
                    table.CheckConstraint("CK_Perfumes_Concentration", "[Concentration] IN (N'Parfum', N'EDP', N'EDT', N'EDC')");
                    table.CheckConstraint("CK_Perfumes_Gender", "[Gender] IN (N'Women', N'Men', N'Unisex')");
                    table.CheckConstraint("CK_Perfumes_Price", "[Price] > 0");
                    table.CheckConstraint("CK_Perfumes_Season", "[Season] IN (N'Spring', N'Summer', N'Autumn', N'Winter', N'AllYear')");
                    table.CheckConstraint("CK_Perfumes_Volume", "[VolumeMl] > 0");
                    table.ForeignKey(
                        name: "FK_Perfumes_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PerfumeDetails",
                columns: table => new
                {
                    PerfumeId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Perfumer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LaunchYear = table.Column<short>(type: "smallint", nullable: true),
                    Longevity = table.Column<byte>(type: "tinyint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfumeDetails", x => x.PerfumeId);
                    table.CheckConstraint("CK_PerfumeDetails_Longevity", "[Longevity] BETWEEN 1 AND 5");
                    table.CheckConstraint("CK_PerfumeDetails_Year", "[LaunchYear] BETWEEN 1800 AND 2100");
                    table.ForeignKey(
                        name: "FK_PerfumeDetails_Perfumes_PerfumeId",
                        column: x => x.PerfumeId,
                        principalTable: "Perfumes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PerfumeImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PerfumeId = table.Column<int>(type: "int", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsMain = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfumeImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerfumeImages_Perfumes_PerfumeId",
                        column: x => x.PerfumeId,
                        principalTable: "Perfumes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PerfumeNotes",
                columns: table => new
                {
                    PerfumeId = table.Column<int>(type: "int", nullable: false),
                    NoteId = table.Column<int>(type: "int", nullable: false),
                    Layer = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfumeNotes", x => new { x.PerfumeId, x.NoteId });
                    table.CheckConstraint("CK_PerfumeNotes_Layer", "[Layer] IN (N'Top', N'Heart', N'Base')");
                    table.ForeignKey(
                        name: "FK_PerfumeNotes_Notes_NoteId",
                        column: x => x.NoteId,
                        principalTable: "Notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerfumeNotes_Perfumes_PerfumeId",
                        column: x => x.PerfumeId,
                        principalTable: "Perfumes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_Brands_Name",
                table: "Brands",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notes_Family",
                table: "Notes",
                column: "Family")
                .Annotation("SqlServer:Include", new[] { "Name" });

            migrationBuilder.CreateIndex(
                name: "UQ_Notes_Name",
                table: "Notes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerfumeImages_Perfume_Sort",
                table: "PerfumeImages",
                columns: new[] { "PerfumeId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "UX_PerfumeImages_OneMain",
                table: "PerfumeImages",
                column: "PerfumeId",
                unique: true,
                filter: "[IsMain] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PerfumeNotes_NoteId",
                table: "PerfumeNotes",
                columns: new[] { "NoteId", "PerfumeId" })
                .Annotation("SqlServer:Include", new[] { "Layer" });

            migrationBuilder.CreateIndex(
                name: "IX_Perfumes_BrandId",
                table: "Perfumes",
                column: "BrandId",
                filter: "[IsDeleted] = 0")
                .Annotation("SqlServer:Include", new[] { "Name", "Price" });

            migrationBuilder.CreateIndex(
                name: "IX_Perfumes_Gender_Price",
                table: "Perfumes",
                columns: new[] { "Gender", "Price" },
                filter: "[IsActive] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Perfumes_Name",
                table: "Perfumes",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Perfumes_Season",
                table: "Perfumes",
                column: "Season",
                filter: "[IsActive] = 1 AND [IsDeleted] = 0")
                .Annotation("SqlServer:Include", new[] { "Name", "Price", "BrandId" });

            migrationBuilder.CreateIndex(
                name: "UQ_Perfumes_Brand_Name_Volume",
                table: "Perfumes",
                columns: new[] { "BrandId", "Name", "VolumeMl" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Perfumes_Sku",
                table: "Perfumes",
                column: "Sku",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PerfumeDetails");

            migrationBuilder.DropTable(
                name: "PerfumeImages");

            migrationBuilder.DropTable(
                name: "PerfumeNotes");

            migrationBuilder.DropTable(
                name: "Notes");

            migrationBuilder.DropTable(
                name: "Perfumes");

            migrationBuilder.DropTable(
                name: "Brands");
        }
    }
}
