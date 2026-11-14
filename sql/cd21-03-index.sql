CREATE INDEX IX_Orders_Institution_CreatedAt_Id
    ON dbo.Orders (InstitutionId, CreatedAt DESC, Id DESC)
    INCLUDE (TotalAmount);
GO
