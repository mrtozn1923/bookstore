CREATE OR ALTER FUNCTION dbo.fn_DiscountedPrice (@price decimal(10,2), @percent int)
RETURNS decimal(10,2)
AS
BEGIN
    RETURN ROUND(@price * (100 - @percent) / 100.0, 2);
END;
GO

CREATE OR ALTER FUNCTION dbo.fn_BestSellers (@since datetime2)
RETURNS TABLE
AS
RETURN
    SELECT oi.BookId, SUM(oi.Quantity) AS Units
    FROM dbo.OrderItems AS oi
    JOIN dbo.Orders AS o ON o.Id = oi.OrderId
    WHERE o.CreatedAt >= @since
    GROUP BY oi.BookId;
