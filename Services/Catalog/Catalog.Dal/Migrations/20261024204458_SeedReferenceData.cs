using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Catalog.Dal.Migrations
{
    /// <inheritdoc />
    public partial class SeedReferenceData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Brands",
                columns: new[] { "Id", "Country", "FoundedYear", "Name" },
                values: new object[,]
                {
                    { 1, "France", (short)1952, "Maison Aurelle" },
                    { 2, "Sweden", (short)2009, "Nordic Bloom" },
                    { 3, "Italy", (short)1987, "Velvet Atelier" },
                    { 4, "Spain", (short)1999, "Casa Ambra" },
                    { 5, "UAE", (short)2014, "Oud & Co" }
                });

            migrationBuilder.InsertData(
                table: "Notes",
                columns: new[] { "Id", "Family", "Name" },
                values: new object[,]
                {
                    { 1, "Citrus", "Bergamot" },
                    { 2, "Citrus", "Lemon" },
                    { 3, "Citrus", "Grapefruit" },
                    { 4, "Fruity", "Cherry" },
                    { 5, "Fruity", "Apple" },
                    { 6, "Fruity", "Fig" },
                    { 7, "Gourmand", "Pumpkin" },
                    { 8, "Gourmand", "Vanilla" },
                    { 9, "Gourmand", "Caramel" },
                    { 10, "Gourmand", "Coffee" },
                    { 11, "Spicy", "Cinnamon" },
                    { 12, "Spicy", "Clove" },
                    { 13, "Spicy", "Cardamom" },
                    { 14, "Spicy", "Pink Pepper" },
                    { 15, "Floral", "Rose" },
                    { 16, "Floral", "Iris" },
                    { 17, "Floral", "Jasmine" },
                    { 18, "Floral", "Cherry Blossom" },
                    { 19, "Woody", "Cedar" },
                    { 20, "Woody", "Sandalwood" },
                    { 21, "Woody", "Patchouli" },
                    { 22, "Woody", "Oud" },
                    { 23, "Woody", "Vetiver" },
                    { 24, "Amber", "Amber" },
                    { 25, "Musk", "Musk" },
                    { 26, "Aquatic", "Sea Salt" },
                    { 27, "Green", "Mint" },
                    { 28, "Smoky", "Smoke" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Brands",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "Notes",
                keyColumn: "Id",
                keyValue: 28);
        }
    }
}
