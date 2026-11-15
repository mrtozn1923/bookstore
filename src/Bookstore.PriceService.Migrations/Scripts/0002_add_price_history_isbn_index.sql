CREATE INDEX IX_PriceHistory_Isbn_ValidFrom ON dbo.PriceHistory (Isbn, ValidFrom DESC) INCLUDE (Price, Currency);
