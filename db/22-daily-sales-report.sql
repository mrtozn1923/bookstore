CREATE TABLE dbo.DailySalesReport
(
    ReportDate date NOT NULL,
    Category varchar(30) NOT NULL,
    Orders int NOT NULL,
    Items int NOT NULL,
    Revenue decimal(14,2) NOT NULL,
    CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_DailySalesReport_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_DailySalesReport PRIMARY KEY (ReportDate, Category)
);
GO
CREATE OR ALTER PROCEDURE dbo.FillDailySalesReport @ReportDate date = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @ReportDate = ISNULL(@ReportDate, DATEADD(DAY, -1, CAST(SYSUTCDATETIME() AS date)));
    IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE CreatedAt >= @ReportDate AND CreatedAt < DATEADD(DAY, 1, @ReportDate))
        THROW 50001, 'No orders found for the report date; the order import may not have run.', 1;
    BEGIN TRANSACTION;
    DELETE FROM dbo.DailySalesReport WHERE ReportDate = @ReportDate;
    INSERT dbo.DailySalesReport (ReportDate, Category, Orders, Items, Revenue)
    SELECT @ReportDate, b.Category, COUNT(DISTINCT o.OrderId), SUM(i.Quantity), SUM(i.Quantity * i.UnitPrice)
    FROM dbo.Orders AS o
    JOIN dbo.OrderItems AS i ON i.OrderId = o.OrderId
    JOIN dbo.Books AS b ON b.BookId = i.BookId
    WHERE o.CreatedAt >= @ReportDate AND o.CreatedAt < DATEADD(DAY, 1, @ReportDate)
      AND o.Status <> 'Cancelled'
    GROUP BY b.Category;
    PRINT CONCAT('report rows for ', @ReportDate, ': ', @@ROWCOUNT);
    COMMIT;
END;
