using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookstore.PgLab.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class AddTitleSearchIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_books_search_vector",
                schema: "catalog",
                table: "books",
                column: "search_vector")
                .Annotation("Npgsql:CreatedConcurrently", true)
                .Annotation("Npgsql:IndexMethod", "gin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_books_search_vector",
                schema: "catalog",
                table: "books");
        }
    }
}
