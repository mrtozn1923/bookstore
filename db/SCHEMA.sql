/*
    SQL Server Derinlemesine / SQL Server Deep Dive - shared Bookstore lab schema and data
    Target: SQL Server 2025 (17.x), compatibility level 160+ (GENERATE_SERIES), tested on 170.

    Usage (sqlcmd, run inside an EMPTY or previously loaded sqls_<NN> database):
        CREATE DATABASE sqls_07;
        sqlcmd -S ... -d sqls_07 -I -i SCHEMA.sql

    The script is a single batch (no GO) so the safety guard below can stop everything.
    It is idempotent: it drops and recreates the Bookstore tables in the current database.
    All data is generated deterministically (no RAND/NEWID): two loads produce identical rows.
    See SCHEMA.md for table sizes, distributions and the list of additive changes.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Db sysname = DB_NAME();
IF @Db NOT LIKE 'sqls[_]%'
BEGIN
    RAISERROR('SCHEMA.sql must run inside a sqls_<NN> database. Current database: %s', 16, 1, @Db) WITH NOWAIT;
    RETURN;
END;

IF (SELECT compatibility_level FROM sys.databases WHERE name = DB_NAME()) < 160
BEGIN
    RAISERROR('Compatibility level 160 or higher is required (GENERATE_SERIES).', 16, 1) WITH NOWAIT;
    RETURN;
END;

-- Lab database settings (database scope only, never server scope)
ALTER DATABASE CURRENT SET RECOVERY SIMPLE;

DECLARE @AuthorCount   int = 5000;
DECLARE @BookCount     int = 40000;
DECLARE @CustomerCount int = 100000;
DECLARE @OrderCount    int = 1000000;
DECLARE @Start         datetime2(0) = '2023-01-01T00:00:00';
DECLARE @SecondsPerOrder int = 120;   -- 1,000,000 orders * 120 s: 2023-01-01 .. 2026-10-20
DECLARE @P bigint = 2147483647;       -- Mersenne prime 2^31-1 used by the deterministic hash

/* ---------- drop (child tables first) ---------- */
DROP TABLE IF EXISTS dbo.Shipments;
DROP TABLE IF EXISTS dbo.Payments;
DROP TABLE IF EXISTS dbo.OrderItems;
DROP TABLE IF EXISTS dbo.Orders;
DROP TABLE IF EXISTS dbo.StockItems;
DROP TABLE IF EXISTS dbo.Books;
DROP TABLE IF EXISTS dbo.Authors;
DROP TABLE IF EXISTS dbo.Customers;

/* ---------- tables (EF Core style names: plural tables, <Entity>Id keys) ---------- */
CREATE TABLE dbo.Authors
(
    AuthorId  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Authors PRIMARY KEY CLUSTERED,
    Name      nvarchar(100) NOT NULL,
    Country   char(2) NOT NULL
);

CREATE TABLE dbo.Books
(
    BookId        int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Books PRIMARY KEY CLUSTERED,
    Isbn          varchar(13) NOT NULL,
    Title         nvarchar(200) NOT NULL,
    AuthorId      int NOT NULL,
    Category      varchar(30) NOT NULL,
    Price         decimal(10,2) NOT NULL,
    PublishedYear smallint NOT NULL
);

CREATE TABLE dbo.Customers
(
    CustomerId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY CLUSTERED,
    FirstName  nvarchar(50) NOT NULL,
    LastName   nvarchar(50) NOT NULL,
    Email      varchar(200) NOT NULL,
    Country    char(2) NOT NULL,
    City       nvarchar(60) NOT NULL,
    CreatedAt  datetime2(0) NOT NULL
);

CREATE TABLE dbo.Orders
(
    OrderId     int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Orders PRIMARY KEY CLUSTERED,
    OrderNumber varchar(20) NOT NULL,
    CustomerId  int NOT NULL,
    Status      varchar(20) NOT NULL,
    CreatedAt   datetime2(0) NOT NULL,
    TotalAmount decimal(12,2) NOT NULL,
    Currency    char(3) NOT NULL
);

CREATE TABLE dbo.OrderItems
(
    OrderItemId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderItems PRIMARY KEY CLUSTERED,
    OrderId     int NOT NULL,
    BookId      int NOT NULL,
    Quantity    smallint NOT NULL,
    UnitPrice   decimal(10,2) NOT NULL
);

CREATE TABLE dbo.StockItems
(
    BookId           int NOT NULL CONSTRAINT PK_StockItems PRIMARY KEY CLUSTERED,
    QuantityOnHand   int NOT NULL,
    QuantityReserved int NOT NULL,
    UpdatedAt        datetime2(0) NOT NULL
);

CREATE TABLE dbo.Payments
(
    PaymentId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Payments PRIMARY KEY CLUSTERED,
    OrderId   int NOT NULL,
    Amount    decimal(12,2) NOT NULL,
    Currency  char(3) NOT NULL,
    Method    varchar(20) NOT NULL,
    Status    varchar(20) NOT NULL,
    CreatedAt datetime2(0) NOT NULL
);

CREATE TABLE dbo.Shipments
(
    ShipmentId     int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Shipments PRIMARY KEY CLUSTERED,
    OrderId        int NOT NULL,
    Carrier        varchar(30) NOT NULL,
    TrackingNumber varchar(30) NOT NULL,
    ShippedAt      datetime2(0) NOT NULL,
    DeliveredAt    datetime2(0) NULL
);

/* ---------- reference lists (people names and classic titles, see DOMAIN.md) ---------- */
DECLARE @FirstNames TABLE (i int PRIMARY KEY, Name nvarchar(50) NOT NULL);
INSERT @FirstNames VALUES
    (0,N'Ada'),(1,N'Alan'),(2,N'Grace'),(3,N'Linus'),(4,N'Barbara'),(5,N'Edsger'),(6,N'Donald'),
    (7,N'Margaret'),(8,N'Ken'),(9,N'Dennis'),(10,N'Frances'),(11,N'John'),(12,N'Katherine'),
    (13,N'Niklaus'),(14,N'Radia'),(15,N'Tim'),(16,N'Hedy'),(17,N'Bjarne'),(18,N'Anita'),(19,N'Guido');

DECLARE @LastNames TABLE (i int PRIMARY KEY, Name nvarchar(50) NOT NULL);
INSERT @LastNames VALUES
    (0,N'Lovelace'),(1,N'Turing'),(2,N'Hopper'),(3,N'Torvalds'),(4,N'Liskov'),(5,N'Dijkstra'),
    (6,N'Knuth'),(7,N'Hamilton'),(8,N'Thompson'),(9,N'Ritchie'),(10,N'Allen'),(11,N'Backus'),
    (12,N'Johnson'),(13,N'Wirth'),(14,N'Perlman'),(15,N'Berners-Lee'),(16,N'Lamarr'),
    (17,N'Stroustrup'),(18,N'Borg'),(19,N'van Rossum');

DECLARE @Titles TABLE (i int PRIMARY KEY, Title nvarchar(100) NOT NULL, Category varchar(30) NOT NULL);
INSERT @Titles VALUES
    (0,N'Dune','SciFi'),(1,N'Emma','Classic'),(2,N'The Hobbit','Fantasy'),(3,N'Middlemarch','Classic'),
    (4,N'Frankenstein','Classic'),(5,N'Foundation','SciFi'),(6,N'Persuasion','Classic'),
    (7,N'The Time Machine','SciFi'),(8,N'Moby-Dick','Classic'),(9,N'Jane Eyre','Classic'),
    (10,N'Dracula','Horror'),(11,N'Ulysses','Classic'),(12,N'War and Peace','Classic'),
    (13,N'Don Quixote','Classic'),(14,N'The Odyssey','Classic'),(15,N'Neuromancer','SciFi'),
    (16,N'Hyperion','SciFi'),(17,N'The Silmarillion','Fantasy'),(18,N'Little Women','Classic'),
    (19,N'Brave New World','SciFi');

DECLARE @Cities TABLE (Country char(2) NOT NULL, i int NOT NULL, City nvarchar(60) NOT NULL, PRIMARY KEY (Country, i));
INSERT @Cities VALUES
    ('TR',0,N'Istanbul'),('TR',1,N'Ankara'),('TR',2,N'Izmir'),
    ('DE',0,N'Berlin'),('DE',1,N'Munich'),('DE',2,N'Hamburg'),
    ('GB',0,N'London'),('GB',1,N'Manchester'),('GB',2,N'Leeds'),
    ('NL',0,N'Amsterdam'),('NL',1,N'Rotterdam'),('NL',2,N'Utrecht'),
    ('FR',0,N'Paris'),('FR',1,N'Lyon'),('FR',2,N'Lille');

/*
    Deterministic hash used everywhere below (no RAND/NEWID, so every load is identical):
        x = (n * 48271 + salt * 69621) mod P,   h(n, salt) = x * x mod P,   P = 2^31 - 1
    h is in [0, P). Several independent-looking "digits" are taken from one h with % and /.
    u = h / P is used as a uniform value in [0, 1); POWER(u, k) creates skew toward small ids.
*/

/* ---------- Authors: 5,000 ---------- */
SET IDENTITY_INSERT dbo.Authors ON;
INSERT dbo.Authors WITH (TABLOCK) (AuthorId, Name, Country)
SELECT g.value,
       CONCAT(f.Name, N' ', l.Name),
       CASE (r.h / 400) % 5 WHEN 0 THEN 'GB' WHEN 1 THEN 'US' WHEN 2 THEN 'FR' WHEN 3 THEN 'DE' ELSE 'TR' END
FROM GENERATE_SERIES(CAST(1 AS bigint), CAST(@AuthorCount AS bigint)) AS g
CROSS APPLY (VALUES ((g.value * 48271 + 1 * 69621) % @P)) AS x(v)
CROSS APPLY (VALUES (x.v * x.v % @P)) AS r(h)
JOIN @FirstNames AS f ON f.i = r.h % 20
JOIN @LastNames  AS l ON l.i = (r.h / 20) % 20
ORDER BY g.value;
SET IDENTITY_INSERT dbo.Authors OFF;

/* ---------- Books: 40,000; AuthorId uniform, Price 5.00 .. 49.99, 20 classic titles x 2,000 ---------- */
SET IDENTITY_INSERT dbo.Books ON;
INSERT dbo.Books WITH (TABLOCK) (BookId, Isbn, Title, AuthorId, Category, Price, PublishedYear)
SELECT g.value,
       CONCAT('978', RIGHT(CONCAT('0000000000', g.value), 10)),
       CONCAT(t.Title, N' #', g.value),
       CAST(1 + r.h % @AuthorCount AS int),
       t.Category,
       CAST(5 + ((r.h / @AuthorCount) % 4500) / 100.0 AS decimal(10,2)),
       CAST(1850 + (r.h / 22500000) % 76 + (r.h % 7) * 15 AS smallint)
FROM GENERATE_SERIES(CAST(1 AS bigint), CAST(@BookCount AS bigint)) AS g
CROSS APPLY (VALUES ((g.value * 48271 + 2 * 69621) % @P)) AS x(v)
CROSS APPLY (VALUES (x.v * x.v % @P)) AS r(h)
JOIN @Titles AS t ON t.i = g.value % 20
ORDER BY g.value;
SET IDENTITY_INSERT dbo.Books OFF;

/* ---------- Customers: 100,000; Country skewed (TR 60%), City correlated with Country ---------- */
SET IDENTITY_INSERT dbo.Customers ON;
WITH c AS
(
    SELECT g.value AS n, r.h,
           CASE WHEN g.value = 1 THEN 'TR'
                WHEN r.h % 100 < 60 THEN 'TR'
                WHEN r.h % 100 < 75 THEN 'DE'
                WHEN r.h % 100 < 85 THEN 'GB'
                WHEN r.h % 100 < 93 THEN 'NL'
                ELSE 'FR' END AS Country,
           -- the first city of each country gets ~70% of that country's customers
           CASE WHEN (r.h / 100) % 10 < 7 THEN 0
                WHEN (r.h / 100) % 10 < 9 THEN 1
                ELSE 2 END AS CityIndex
    FROM GENERATE_SERIES(CAST(1 AS bigint), CAST(@CustomerCount AS bigint)) AS g
    CROSS APPLY (VALUES ((g.value * 48271 + 4 * 69621) % @P)) AS x(v)
    CROSS APPLY (VALUES (x.v * x.v % @P)) AS r(h)
)
INSERT dbo.Customers WITH (TABLOCK) (CustomerId, FirstName, LastName, Email, Country, City, CreatedAt)
SELECT c.n, f.Name, l.Name,
       CONCAT('customer', c.n, '@example.com'),
       c.Country, ci.City,
       DATEADD(MINUTE, CAST(c.n * 9 AS int), CAST('2021-01-01T00:00:00' AS datetime2(0)))
FROM c
JOIN @FirstNames AS f  ON f.i = (c.h / 1000) % 20
JOIN @LastNames  AS l  ON l.i = (c.h / 20000) % 20
JOIN @Cities     AS ci ON ci.Country = c.Country AND ci.i = c.CityIndex
ORDER BY c.n;
SET IDENTITY_INSERT dbo.Customers OFF;

-- Customer 1 is the big B2B account (a university library) used by the parameter sniffing demos
UPDATE dbo.Customers SET FirstName = N'Grace', LastName = N'Hopper', City = N'Istanbul' WHERE CustomerId = 1;

/* ---------- Orders: 1,000,000; CustomerId heavily skewed, CreatedAt ascending with OrderId ---------- */
SET IDENTITY_INSERT dbo.Orders ON;
WITH o AS
(
    SELECT g.value AS n,
           CASE WHEN g.value % 5 = 0 THEN 1   -- customer 1 owns exactly 20% of all orders
                -- the rest: power-law, small CustomerId values get many more orders
                ELSE 2 + CAST(FLOOR((@CustomerCount - 1) * POWER(CAST(r.h AS float) / @P, 3)) AS int)
           END AS CustomerId
    FROM GENERATE_SERIES(CAST(1 AS bigint), CAST(@OrderCount AS bigint)) AS g
    CROSS APPLY (VALUES ((g.value * 48271 + 5 * 69621) % @P)) AS x(v)
    CROSS APPLY (VALUES (x.v * x.v % @P)) AS r(h)
)
INSERT dbo.Orders WITH (TABLOCK) (OrderId, OrderNumber, CustomerId, Status, CreatedAt, TotalAmount, Currency)
SELECT o.n,
       CONCAT('BK-', RIGHT(CONCAT('00000000', o.n), 8)),
       cu.CustomerId,
       CASE WHEN o.n % 25 = 7 THEN 'Cancelled'
            WHEN o.n > @OrderCount - 5000 THEN 'Pending'
            WHEN o.n > @OrderCount - 50000 AND o.n % 97 = 0 THEN 'PaymentPending'
            WHEN o.n > @OrderCount - 20000 THEN 'Shipped'
            ELSE 'Delivered' END,
       DATEADD(SECOND, CAST((o.n - 1) * @SecondsPerOrder AS int), @Start),
       0,
       CASE WHEN cu.Country = 'TR' THEN 'TRY' ELSE 'EUR' END
FROM o
JOIN dbo.Customers AS cu
    ON cu.CustomerId = CASE WHEN o.CustomerId > @CustomerCount THEN @CustomerCount ELSE o.CustomerId END
ORDER BY o.n;
SET IDENTITY_INSERT dbo.Orders OFF;

/* ---------- OrderItems: 1..4 lines per order (avg 2.5), BookId popularity skewed ---------- */
INSERT dbo.OrderItems WITH (TABLOCK) (OrderId, BookId, Quantity, UnitPrice)
SELECT x.OrderId, b.BookId,
       CASE WHEN x.h % 50 = 1 THEN 3 WHEN x.h % 10 = 0 THEN 2 ELSE 1 END,
       b.Price
FROM
(
    SELECT o.OrderId, v.i, ri.h,
           1 + CAST(FLOOR((@BookCount - 1) * POWER(CAST(ri.h AS float) / @P, 2)) AS int) AS BookId
    FROM dbo.Orders AS o
    CROSS APPLY (VALUES ((CAST(o.OrderId AS bigint) * 48271 + 6 * 69621) % @P)) AS xo(v)
    CROSS APPLY (VALUES (xo.v * xo.v % @P)) AS ro(h)
    JOIN (VALUES (1),(2),(3),(4)) AS v(i) ON v.i <= 1 + ro.h % 4
    CROSS APPLY (VALUES (((CAST(o.OrderId AS bigint) * 4 + v.i) * 48271 + 7 * 69621) % @P)) AS xi(v)
    CROSS APPLY (VALUES (xi.v * xi.v % @P)) AS ri(h)
) AS x
JOIN dbo.Books AS b ON b.BookId = x.BookId
ORDER BY x.OrderId, x.i;

UPDATE o
SET    TotalAmount = t.Total
FROM   dbo.Orders AS o
JOIN  (SELECT OrderId, SUM(Quantity * UnitPrice) AS Total FROM dbo.OrderItems GROUP BY OrderId) AS t
       ON t.OrderId = o.OrderId;

/* ---------- StockItems: one row per book ---------- */
INSERT dbo.StockItems WITH (TABLOCK) (BookId, QuantityOnHand, QuantityReserved, UpdatedAt)
SELECT b.BookId, CAST(r.h % 200 AS int), 0, '2026-10-31T00:00:00'
FROM dbo.Books AS b
CROSS APPLY (VALUES ((CAST(b.BookId AS bigint) * 48271 + 9 * 69621) % @P)) AS x(v)
CROSS APPLY (VALUES (x.v * x.v % @P)) AS r(h)
ORDER BY b.BookId;

/* ---------- Payments: one per order, PaymentId = OrderId ---------- */
SET IDENTITY_INSERT dbo.Payments ON;
INSERT dbo.Payments WITH (TABLOCK) (PaymentId, OrderId, Amount, Currency, Method, Status, CreatedAt)
SELECT o.OrderId, o.OrderId, o.TotalAmount, o.Currency,
       CASE WHEN r.h % 10 < 6 THEN 'Card' WHEN r.h % 10 < 9 THEN 'BankTransfer' ELSE 'Wallet' END,
       CASE o.Status WHEN 'Cancelled' THEN 'Refunded'
                     WHEN 'PaymentPending' THEN 'Authorized'
                     WHEN 'Pending' THEN 'Authorized'
                     ELSE 'Captured' END,
       DATEADD(MINUTE, 2, o.CreatedAt)
FROM dbo.Orders AS o
CROSS APPLY (VALUES ((CAST(o.OrderId AS bigint) * 48271 + 10 * 69621) % @P)) AS x(v)
CROSS APPLY (VALUES (x.v * x.v % @P)) AS r(h)
ORDER BY o.OrderId;
SET IDENTITY_INSERT dbo.Payments OFF;

/* ---------- Shipments: for Shipped and Delivered orders ---------- */
INSERT dbo.Shipments WITH (TABLOCK) (OrderId, Carrier, TrackingNumber, ShippedAt, DeliveredAt)
SELECT o.OrderId,
       CASE WHEN o.OrderId % 3 = 0 THEN 'Express' ELSE 'Standard' END,
       CONCAT('TRK', RIGHT(CONCAT('0000000000', o.OrderId), 10)),
       DATEADD(HOUR, 24 + CAST(r.h % 48 AS int), o.CreatedAt),
       CASE WHEN o.Status = 'Delivered'
            THEN DATEADD(HOUR, 72 + CAST((r.h / 48) % 96 AS int), o.CreatedAt) END
FROM dbo.Orders AS o
CROSS APPLY (VALUES ((CAST(o.OrderId AS bigint) * 48271 + 11 * 69621) % @P)) AS x(v)
CROSS APPLY (VALUES (x.v * x.v % @P)) AS r(h)
WHERE o.Status IN ('Shipped', 'Delivered')
ORDER BY o.OrderId;

/* ---------- indexes (what EF Core conventions would create) and constraints ---------- */
CREATE UNIQUE NONCLUSTERED INDEX UX_Books_Isbn ON dbo.Books (Isbn);
CREATE NONCLUSTERED INDEX IX_Books_AuthorId ON dbo.Books (AuthorId);
CREATE UNIQUE NONCLUSTERED INDEX UX_Customers_Email ON dbo.Customers (Email);
CREATE UNIQUE NONCLUSTERED INDEX UX_Orders_OrderNumber ON dbo.Orders (OrderNumber);
CREATE NONCLUSTERED INDEX IX_Orders_CustomerId ON dbo.Orders (CustomerId);
CREATE NONCLUSTERED INDEX IX_OrderItems_OrderId ON dbo.OrderItems (OrderId);
CREATE NONCLUSTERED INDEX IX_OrderItems_BookId ON dbo.OrderItems (BookId);
CREATE UNIQUE NONCLUSTERED INDEX UX_Payments_OrderId ON dbo.Payments (OrderId);
CREATE UNIQUE NONCLUSTERED INDEX UX_Shipments_OrderId ON dbo.Shipments (OrderId);

ALTER TABLE dbo.Books      ADD CONSTRAINT FK_Books_Authors        FOREIGN KEY (AuthorId)   REFERENCES dbo.Authors (AuthorId);
ALTER TABLE dbo.Orders     ADD CONSTRAINT FK_Orders_Customers     FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (CustomerId);
ALTER TABLE dbo.OrderItems ADD CONSTRAINT FK_OrderItems_Orders    FOREIGN KEY (OrderId)    REFERENCES dbo.Orders (OrderId);
ALTER TABLE dbo.OrderItems ADD CONSTRAINT FK_OrderItems_Books     FOREIGN KEY (BookId)     REFERENCES dbo.Books (BookId);
ALTER TABLE dbo.StockItems ADD CONSTRAINT FK_StockItems_Books     FOREIGN KEY (BookId)     REFERENCES dbo.Books (BookId);
ALTER TABLE dbo.Payments   ADD CONSTRAINT FK_Payments_Orders      FOREIGN KEY (OrderId)    REFERENCES dbo.Orders (OrderId);
ALTER TABLE dbo.Shipments  ADD CONSTRAINT FK_Shipments_Orders     FOREIGN KEY (OrderId)    REFERENCES dbo.Orders (OrderId);
ALTER TABLE dbo.Orders     ADD CONSTRAINT CK_Orders_Status
    CHECK (Status IN ('Pending', 'PaymentPending', 'Shipped', 'Delivered', 'Cancelled'));
ALTER TABLE dbo.Orders     ADD CONSTRAINT CK_Orders_Currency CHECK (Currency IN ('EUR', 'TRY'));
ALTER TABLE dbo.StockItems ADD CONSTRAINT CK_StockItems_Quantity CHECK (QuantityOnHand >= 0 AND QuantityReserved >= 0);

CHECKPOINT;

SELECT t.name AS TableName, SUM(p.rows) AS [Rows]
FROM sys.tables AS t
JOIN sys.partitions AS p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
WHERE t.schema_id = SCHEMA_ID('dbo')
GROUP BY t.name
ORDER BY t.name;
