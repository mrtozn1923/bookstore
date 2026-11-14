SET IDENTITY_INSERT dbo.OrdersP ON;
INSERT INTO dbo.OrdersP WITH (TABLOCK) (OrderId, CustomerId, Status, TotalAmount, Currency, CreatedAt)
SELECT OrderId, CustomerId, Status, TotalAmount, Currency, CreatedAt FROM dbo.Orders;
SET IDENTITY_INSERT dbo.OrdersP OFF;

INSERT INTO dbo.OrderItemsP WITH (TABLOCK) (OrderId, LineNumber, CreatedAt, BookId, Quantity, UnitPrice)
SELECT oi.OrderId, oi.LineNumber, o.CreatedAt, oi.BookId, oi.Quantity, oi.UnitPrice
FROM dbo.OrderItems AS oi
JOIN dbo.Orders AS o ON o.OrderId = oi.OrderId;
GO
