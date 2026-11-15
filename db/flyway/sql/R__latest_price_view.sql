CREATE OR ALTER VIEW dbo.LatestPrice
AS
SELECT Isbn, Price, Currency, ValidFrom, CreatedAt
FROM (
    SELECT Isbn, Price, Currency, ValidFrom, CreatedAt,
           ROW_NUMBER() OVER (PARTITION BY Isbn ORDER BY ValidFrom DESC) AS rn
    FROM dbo.PriceHistory
) AS p
WHERE rn = 1;
