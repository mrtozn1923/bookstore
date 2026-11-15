using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookstore.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookSubtitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Subtitle",
                schema: "catalog",
                table: "Books",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Books",
                keyColumn: "Id",
                keyValue: 1,
                column: "Subtitle",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Books",
                keyColumn: "Id",
                keyValue: 2,
                column: "Subtitle",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Books",
                keyColumn: "Id",
                keyValue: 3,
                column: "Subtitle",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Books",
                keyColumn: "Id",
                keyValue: 4,
                column: "Subtitle",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Books",
                keyColumn: "Id",
                keyValue: 5,
                column: "Subtitle",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Books",
                keyColumn: "Id",
                keyValue: 6,
                column: "Subtitle",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Books",
                keyColumn: "Id",
                keyValue: 7,
                column: "Subtitle",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Books",
                keyColumn: "Id",
                keyValue: 8,
                column: "Subtitle",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Books",
                keyColumn: "Id",
                keyValue: 9,
                column: "Subtitle",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Books",
                keyColumn: "Id",
                keyValue: 10,
                column: "Subtitle",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Books",
                keyColumn: "Id",
                keyValue: 11,
                column: "Subtitle",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Books",
                keyColumn: "Id",
                keyValue: 12,
                column: "Subtitle",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Books",
                keyColumn: "Id",
                keyValue: 13,
                column: "Subtitle",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Books",
                keyColumn: "Id",
                keyValue: 14,
                column: "Subtitle",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Books",
                keyColumn: "Id",
                keyValue: 15,
                column: "Subtitle",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Books",
                keyColumn: "Id",
                keyValue: 16,
                column: "Subtitle",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Books",
                keyColumn: "Id",
                keyValue: 17,
                column: "Subtitle",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Books",
                keyColumn: "Id",
                keyValue: 18,
                column: "Subtitle",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Books",
                keyColumn: "Id",
                keyValue: 19,
                column: "Subtitle",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Books",
                keyColumn: "Id",
                keyValue: 20,
                column: "Subtitle",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Subtitle",
                schema: "catalog",
                table: "Books");
        }
    }
}
