CREATE INDEX IX_Orders_CustomerId_CreatedAt ON dbo.Orders (CustomerId, CreatedAt DESC) INCLUDE (Status, TotalAmount);
