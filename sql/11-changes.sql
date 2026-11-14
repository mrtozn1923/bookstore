SET NOCOUNT ON;

UPDATE dbo.Books SET Price = 189.90 WHERE Isbn = '9781000000030';
UPDATE dbo.Books SET StockQuantity = 0 WHERE Isbn = '9781000000221';

SELECT Isbn, Title, Price, StockQuantity, RowVer FROM dbo.Books WHERE Isbn IN ('9781000000030', '9781000000221');
