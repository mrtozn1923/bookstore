using Bookstore.Data.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookstore.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReportingObjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                EXEC(N'
                CREATE OR ALTER FUNCTION dbo.fn_DiscountedPrice (@price decimal(10,2), @percent int)
                RETURNS decimal(10,2)
                AS
                BEGIN
                    RETURN ROUND(@price * (100 - @percent) / 100.0, 2);
                END');
                """);

            migrationBuilder.Sql("""
                EXEC(N'
                CREATE OR ALTER FUNCTION dbo.fn_BestSellers (@since datetime2)
                RETURNS TABLE
                AS
                RETURN
                    SELECT oi.BookId, SUM(oi.Quantity) AS Units
                    FROM dbo.OrderItems AS oi
                    JOIN dbo.Orders AS o ON o.Id = oi.OrderId
                    WHERE o.CreatedAt >= @since
                    GROUP BY oi.BookId');
                """);

            migrationBuilder.CreateOrAlterView("catalog", "vw_BookSales", """
                SELECT b.Id AS BookId, b.Title, SUM(oi.Quantity) AS Units
                FROM catalog.Books AS b
                JOIN dbo.OrderItems AS oi ON oi.BookId = b.Id
                GROUP BY b.Id, b.Title
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropView("catalog", "vw_BookSales");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS dbo.fn_BestSellers;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS dbo.fn_DiscountedPrice;");
        }
    }
}
