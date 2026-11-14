-- Post 05: changes the search sync should pick up.
SET NOCOUNT ON;

-- A price change and a stock change on the English Dune
UPDATE dbo.Books SET Price = 545.00, StockQuantity = 12 WHERE Isbn = '9781000000221';

-- Moby-Dick is no longer sold: it must disappear from search
UPDATE dbo.Books SET IsDiscontinued = 1 WHERE Isbn = '9781000000337';

SELECT Isbn, Title, Price, StockQuantity, IsDiscontinued, RowVer FROM dbo.Books WHERE Isbn IN ('9781000000221', '9781000000337');
