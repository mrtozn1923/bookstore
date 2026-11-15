using Bookstore.Prompts.Tasks.ReportSql;

namespace Bookstore.Prompts.Tests.ReportSql;

public class SqlGuardTests
{
    [Fact]
    public void Accepts_a_select_on_reporting_views()
    {
        SqlCheck check = SqlGuard.Check("""
            SELECT TOP (10) b.Title, SUM(oi.Quantity) AS Sold
            FROM reporting.Books AS b
            JOIN reporting.OrderItems AS oi ON oi.BookId = b.Id
            JOIN reporting.Orders AS o ON o.Id = oi.OrderId
            WHERE o.OrderedAt >= @StartDate AND o.OrderedAt < @EndDate
            GROUP BY b.Title
            ORDER BY Sold DESC
            """);

        Assert.True(check.Ok, string.Join("; ", check.Errors));
    }

    [Theory]
    [InlineData("SELECT TOP 5 * FROM reporting.Books ORDER BY Price DESC; DROP TABLE reporting.Orders; --", "expected exactly one statement, got 2")]
    [InlineData("DELETE FROM reporting.Orders WHERE Status = 'Cancelled'", "only SELECT is allowed, got DeleteStatement")]
    [InlineData("SELECT Title FROM reporting.Books LIMIT 10", "syntax error at line 1")]
    [InlineData("SELECT name FROM sys.tables", "schema sys is not allowed")]
    [InlineData("SELECT c.Email FROM reporting.Customers AS c", "unknown column c.Email")]
    [InlineData("SELECT reporting.Books.Name FROM reporting.Books", "unknown column Books.Name")]
    [InlineData("SELECT Id INTO #copy FROM reporting.Orders", "SELECT INTO is not allowed")]
    [InlineData("SELECT * FROM OPENROWSET('SQLNCLI', 'Server=x;', 'SELECT 1') AS r", "table source OpenRowsetTableReference is not allowed")]
    [InlineData("SELECT COUNT(*) FROM reporting.Orders WHERE OrderedAt >= '2023-01-01'", "date literal '2023-01-01'")]
    [InlineData("SELECT COUNT(*) FROM reporting.Orders WHERE OrderedAt BETWEEN @StartDate AND @EndDate", "@EndDate is exclusive")]
    [InlineData("SELECT COUNT(*) FROM reporting.Orders WHERE CustomerId = @CustomerId", "unknown parameter @CustomerId")]
    public void Rejects(string sql, string error)
    {
        SqlCheck check = SqlGuard.Check(sql);

        Assert.False(check.Ok);
        Assert.Contains(check.Errors, e => e.StartsWith(error, StringComparison.Ordinal));
    }

    [Fact]
    public void Names_defined_by_a_cte_are_allowed()
    {
        SqlCheck check = SqlGuard.Check("""
            WITH Stock AS (SELECT BookId, SUM(Quantity) AS Total FROM reporting.StockItems GROUP BY BookId)
            SELECT b.Title, s.Total FROM Stock AS s JOIN reporting.Books AS b ON b.Id = s.BookId WHERE s.Total < 5
            """);

        Assert.True(check.Ok, string.Join("; ", check.Errors));
    }

    [Fact]
    public void Free_text_answers_are_read_from_the_first_sql_block()
    {
        ReportSqlAnswer answer = ReportSqlGrader.ParseAnswer("Here you go:\n```sql\nSELECT 1\n```\nHope it helps.");

        Assert.Equal("SELECT 1", answer.Sql);
    }

    [Fact]
    public void A_refuse_decision_never_carries_sql()
    {
        ReportSqlAnswer answer = ReportSqlGrader.ParseAnswer("""{"decision":"refuse","reason":"no email column","sql":"SELECT 1"}""");

        Assert.Null(answer.Sql);
        Assert.Equal("no email column", answer.Refusal);
    }
}
