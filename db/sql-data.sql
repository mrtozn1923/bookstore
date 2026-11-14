SET NOCOUNT ON;

WITH n AS (SELECT TOP (1000000) CAST(ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS int) AS g
           FROM sys.all_columns AS a CROSS JOIN sys.all_columns AS b)
SELECT g INTO #n FROM n;

INSERT INTO dbo.Books (BookId, Isbn, Title, Category, Price)
SELECT g, CAST(9780000000000 + g AS char(13)),
       CHOOSE(1 + g % 8, N'Dune', N'Emma', N'The Hobbit', N'Middlemarch', N'Persuasion', N'Ulysses', N'Beloved', N'Walden') + N' #' + CAST(g AS nvarchar(10)),
       CHOOSE(1 + g % 5, 'Fantasy', 'Classics', 'Science', 'History', 'Poetry'),
       80 + (g * 37) % 420
FROM #n WHERE g <= 20000;

INSERT INTO dbo.Customers (CustomerId, Email, FullName, Country, CreatedAt)
SELECT g, 'customer' + CAST(g AS varchar(10)) + '@example.com',
       CHOOSE(1 + g % 6, N'Ada', N'Alan', N'Grace', N'Linus', N'Barbara', N'Edsger') + N' ' + CAST(g AS nvarchar(10)),
       CHOOSE(1 + g % 6, 'TR', 'TR', 'TR', 'DE', 'NL', 'GB'),
       DATEADD(MINUTE, g * 20, '2022-11-01')
FROM #n WHERE g <= 100000;

INSERT INTO dbo.Orders WITH (TABLOCK) (CustomerId, Status, TotalAmount, Currency, CreatedAt)
SELECT 1 + (CAST(g AS bigint) * 7919) % 100000,
       CASE WHEN g % 50 = 0 THEN 'Cancelled' WHEN g > 990000 THEN 'Pending' ELSE 'Delivered' END,
       0, 'TRY',
       DATEADD(SECOND, CAST(126230400.0 * SQRT(g / 1000000.0) AS int), '2022-11-14')
FROM #n ORDER BY g;

INSERT INTO dbo.OrderItems WITH (TABLOCK) (OrderId, LineNumber, BookId, Quantity, UnitPrice)
SELECT o.OrderId, l.LineNumber, b.BookId, 1 + (o.OrderId + l.LineNumber) % 3, b.Price
FROM dbo.Orders AS o
JOIN (VALUES (CAST(1 AS smallint)), (2), (3), (4)) AS l(LineNumber) ON l.LineNumber <= 1 + o.OrderId % 4
JOIN dbo.Books AS b ON b.BookId = 1 + (o.OrderId * 31 + l.LineNumber * 977) % 20000;

UPDATE o SET TotalAmount = s.Total
FROM dbo.Orders AS o
JOIN (SELECT OrderId, SUM(Quantity * UnitPrice) AS Total FROM dbo.OrderItems GROUP BY OrderId) AS s ON s.OrderId = o.OrderId;

DROP TABLE #n;
