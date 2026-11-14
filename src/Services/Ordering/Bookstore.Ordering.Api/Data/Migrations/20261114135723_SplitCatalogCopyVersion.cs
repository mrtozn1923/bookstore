using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookstore.Ordering.Data.Migrations
{
    /// <inheritdoc />
    public partial class SplitCatalogCopyVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Version",
                table: "CatalogBooks",
                newName: "TitleVersion");

            migrationBuilder.AddColumn<int>(
                name: "PriceVersion",
                table: "CatalogBooks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Existing rows: both fields were last changed at the old single version.
            migrationBuilder.Sql("UPDATE CatalogBooks SET PriceVersion = TitleVersion;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceVersion",
                table: "CatalogBooks");

            migrationBuilder.RenameColumn(
                name: "TitleVersion",
                table: "CatalogBooks",
                newName: "Version");
        }
    }
}
