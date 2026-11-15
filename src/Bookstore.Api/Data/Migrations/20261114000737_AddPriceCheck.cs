using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookstore.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Raw SQL: not part of the EF Core model, only the migration knows about it
            migrationBuilder.Sql("ALTER TABLE [Books] ADD CONSTRAINT [CK_Books_Price_Positive] CHECK ([Price] > 0);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE [Books] DROP CONSTRAINT [CK_Books_Price_Positive];");
        }
    }
}
