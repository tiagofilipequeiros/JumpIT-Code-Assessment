using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class SeedProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RestartSequence(
                name: "ProductIds",
                startValue: 100100L);

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "CreatedAt", "Description", "Name", "Price", "Stock", "UpdatedAt" },
                values: new object[,]
                {
                    { 100000, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Plan achromat objective, 10x magnification.", "Microscope Objective 10x", 249.90m, 42, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 100001, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Plan achromat objective, 40x magnification.", "Microscope Objective 40x", 389.00m, 18, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 100002, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Oil immersion objective, 100x magnification.", "Microscope Objective 100x Oil", 899.00m, 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 100003, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Wide-field eyepiece, 10x magnification.", "Eyepiece 10x", 79.50m, 120, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 100004, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Replacement LED illumination module.", "LED Illuminator", 159.00m, 0, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 100005, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Pre-cleaned glass slides, 76 x 26 mm.", "Microscope Slides (50 pack)", 12.90m, 500, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 100006, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Square cover glasses, 22 x 22 mm.", "Cover Glasses (100 pack)", 9.90m, 350, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 100007, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Low-fluorescence immersion oil.", "Immersion Oil 20 ml", 24.00m, 7, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 100008, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Adapter for mounting C-mount cameras.", "Camera Adapter C-Mount", 189.00m, 12, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 100009, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Brush, blower and lens tissue.", "Lens Cleaning Kit", 19.90m, 65, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 100010, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Calibration slide, 1 mm in 100 divisions.", "Stage Micrometer", 69.00m, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 100011, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Vinyl dust cover for upright microscopes.", "Dust Cover", 29.00m, 0, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 100000);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 100001);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 100002);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 100003);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 100004);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 100005);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 100006);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 100007);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 100008);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 100009);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 100010);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 100011);

            migrationBuilder.RestartSequence(
                name: "ProductIds",
                startValue: 100000L);
        }
    }
}
