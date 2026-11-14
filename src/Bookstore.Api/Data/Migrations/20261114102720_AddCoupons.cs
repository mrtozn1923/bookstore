using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Bookstore.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCoupons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Coupons",
                schema: "ordering",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    MinSubtotal = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coupons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Coupons_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "catalog",
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "ordering",
                table: "Coupons",
                columns: new[] { "Id", "CategoryId", "Code", "Description", "ExpiresAtUtc", "Kind", "MinSubtotal", "Value" },
                values: new object[,]
                {
                    { 1, null, "BOOK10", "<strong>10%</strong> off your whole cart", new DateTime(2026, 12, 31, 20, 59, 59, 0, DateTimeKind.Utc), "Percent", 0m, 10m },
                    { 2, null, "SAVE50", "<strong>50 TRY</strong> off orders over 300 TRY", new DateTime(2026, 12, 31, 20, 59, 59, 0, DateTimeKind.Utc), "Amount", 300m, 50m },
                    { 3, 1, "CLASSIC15", "<strong>15%</strong> off classics", new DateTime(2026, 12, 31, 20, 59, 59, 0, DateTimeKind.Utc), "Percent", 0m, 15m },
                    { 4, null, "WELCOME25", "<strong>25 TRY</strong> off your first order", new DateTime(2026, 12, 31, 20, 59, 59, 0, DateTimeKind.Utc), "Amount", 0m, 25m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_CategoryId",
                schema: "ordering",
                table: "Coupons",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_Code",
                schema: "ordering",
                table: "Coupons",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Coupons",
                schema: "ordering");
        }
    }
}
