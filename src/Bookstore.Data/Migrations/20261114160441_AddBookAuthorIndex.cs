using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookstore.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookAuthorIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Books_Author",
                table: "Books",
                column: "Author");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Books_Author",
                table: "Books");
        }
    }
}
