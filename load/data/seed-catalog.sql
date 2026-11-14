-- Grows the reference catalog (20 classics) to a production-like size for load tests:
-- 40,000 books, 4,000 authors, stock for every book. Safe to run more than once.
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF (SELECT COUNT(*) FROM catalog.Books) >= 40000
BEGIN
    UPDATE inventory.StockItems SET QuantityOnHand = 1000000 WHERE QuantityOnHand < 1000000;
    PRINT 'Catalog already seeded; stock topped up.';
    RETURN;
END;

BEGIN TRANSACTION;

DECLARE @first TABLE (n int IDENTITY(0, 1), v nvarchar(40));
DECLARE @last TABLE (n int IDENTITY(0, 1), v nvarchar(40));
INSERT @first (v) VALUES (N'Ada'), (N'Alan'), (N'Grace'), (N'Linus'), (N'Edsger'), (N'Barbara'), (N'Donald'), (N'Margaret'),
    (N'Ken'), (N'Frances'), (N'John'), (N'Radia'), (N'Niklaus'), (N'Hedy'), (N'Dennis'), (N'Katherine'), (N'Tim'), (N'Anita'),
    (N'Guido'), (N'Sophie'), (N'Bjarne'), (N'Joan'), (N'James'), (N'Shafi'), (N'Leslie'), (N'Jean'), (N'Vint'), (N'Carol'),
    (N'Peter'), (N'Mary'), (N'Robert'), (N'Lynn'), (N'Tony'), (N'Ruth'), (N'Butler'), (N'Annie'), (N'Edgar'), (N'Evelyn'),
    (N'Andrew'), (N'Lois');
INSERT @last (v) VALUES (N'Lovelace'), (N'Turing'), (N'Hopper'), (N'Torvalds'), (N'Dijkstra'), (N'Liskov'), (N'Knuth'),
    (N'Hamilton'), (N'Thompson'), (N'Allen'), (N'Backus'), (N'Perlman'), (N'Wirth'), (N'Lamarr'), (N'Ritchie'), (N'Johnson'),
    (N'Berners-Lee'), (N'Borg'), (N'van Rossum'), (N'Wilson'), (N'Stroustrup'), (N'Clarke'), (N'Gosling'), (N'Goldwasser'),
    (N'Lamport'), (N'Sammet'), (N'Cerf'), (N'Shaw'), (N'Naur'), (N'Keller'), (N'Floyd'), (N'Conway'), (N'Hoare'), (N'Teitelbaum'),
    (N'Lampson'), (N'Easley'), (N'Codd'), (N'Berezin'), (N'Tanenbaum'), (N'Haibt'), (N'Kay'), (N'Ellis'), (N'Bachman'), (N'Spector'),
    (N'Engelbart'), (N'Sutherland'), (N'Minsky'), (N'McCarthy'), (N'Steele'), (N'Bray'), (N'Fowler'), (N'Beck'), (N'Evans'),
    (N'Martin'), (N'Hunt'), (N'Thomas'), (N'Brooks'), (N'Parnas'), (N'Meyer'), (N'Gamma'), (N'Helm'), (N'Vlissides'), (N'Booch'),
    (N'Jacobson'), (N'Rumbaugh'), (N'Cunningham'), (N'Feathers'), (N'Kernighan'), (N'Pike'), (N'Aho'), (N'Ullman'), (N'Sethi'),
    (N'Hennessy'), (N'Patterson'), (N'Rivest'), (N'Shamir'), (N'Adleman'), (N'Diffie'), (N'Hellman'), (N'Merkle'), (N'Kahn'),
    (N'Postel'), (N'Metcalfe'), (N'Boggs'), (N'Bushnell'), (N'Wozniak'), (N'Raskin'), (N'Atkinson'), (N'Hertzfeld'), (N'Kare'),
    (N'Moore'), (N'Noyce'), (N'Shockley'), (N'Bardeen'), (N'Brattain'), (N'Kilby'), (N'Faggin'), (N'Shima'), (N'Mazor'), (N'Hoff');

DECLARE @adj TABLE (n int IDENTITY(0, 1), v nvarchar(40));
DECLARE @noun TABLE (n int IDENTITY(0, 1), v nvarchar(40));
INSERT @adj (v) VALUES (N'Silent'), (N'Hidden'), (N'Last'), (N'Golden'), (N'Broken'), (N'Distant'), (N'Northern'), (N'Lost'),
    (N'Burning'), (N'Quiet'), (N'Endless'), (N'Forgotten'), (N'Crimson'), (N'Winter'), (N'Iron'), (N'Glass'), (N'Wandering'),
    (N'Secret'), (N'Bright'), (N'Hollow'), (N'Paper'), (N'Salt'), (N'Midnight'), (N'Wild'), (N'Stone'), (N'Little'), (N'Second'),
    (N'Restless'), (N'Shining'), (N'Empty');
INSERT @noun (v) VALUES (N'River'), (N'Garden'), (N'Kingdom'), (N'Harbor'), (N'Library'), (N'Mountain'), (N'Lighthouse'),
    (N'Orchard'), (N'Station'), (N'Island'), (N'Forest'), (N'Archive'), (N'Engine'), (N'Voyage'), (N'Letters'), (N'Summer'),
    (N'Machine'), (N'City'), (N'Bridge'), (N'Desert'), (N'Signal'), (N'Clockmaker'), (N'Cartographer'), (N'Tide'), (N'Promise'),
    (N'Tower'), (N'Road'), (N'Witness'), (N'Market'), (N'Heir'), (N'Map'), (N'Shore'), (N'Storm'), (N'Theory'), (N'Window'),
    (N'Song'), (N'Valley'), (N'Garden Party'), (N'Republic'), (N'Experiment');

-- 4,000 authors: every first/last name pair (40 x 100).
INSERT catalog.Authors (Name)
SELECT f.v + N' ' + l.v FROM @first f CROSS JOIN @last l;

DECLARE @firstAuthorId int = (SELECT MIN(Id) FROM catalog.Authors WHERE Id > 18);

-- 40,000 numbered books: ISBN-13 = '979' + 9-digit sequence + check digit.
WITH numbers AS (
    SELECT TOP (40000 - (SELECT COUNT(*) FROM catalog.Books))
        ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
    FROM sys.all_objects a CROSS JOIN sys.all_objects b
),
isbn12 AS (
    SELECT n, '979' + RIGHT('000000000' + CAST(n AS varchar(9)), 9) AS digits FROM numbers
)
INSERT catalog.Books (Isbn, Title, Description, Price, PublishedYear, AuthorId, CategoryId)
SELECT
    i.digits + CAST((10 - (
        CAST(SUBSTRING(i.digits, 1, 1) AS int) + 3 * CAST(SUBSTRING(i.digits, 2, 1) AS int)
        + CAST(SUBSTRING(i.digits, 3, 1) AS int) + 3 * CAST(SUBSTRING(i.digits, 4, 1) AS int)
        + CAST(SUBSTRING(i.digits, 5, 1) AS int) + 3 * CAST(SUBSTRING(i.digits, 6, 1) AS int)
        + CAST(SUBSTRING(i.digits, 7, 1) AS int) + 3 * CAST(SUBSTRING(i.digits, 8, 1) AS int)
        + CAST(SUBSTRING(i.digits, 9, 1) AS int) + 3 * CAST(SUBSTRING(i.digits, 10, 1) AS int)
        + CAST(SUBSTRING(i.digits, 11, 1) AS int) + 3 * CAST(SUBSTRING(i.digits, 12, 1) AS int)) % 10) % 10 AS varchar(1)),
    a.v + N' ' + o.v + CASE WHEN i.n > 1200 THEN N' ' + CAST(i.n / 1200 + 1 AS nvarchar(10)) ELSE N'' END,
    N'Generated book for load tests.',
    CAST(99 + (i.n * 37) % 900 AS decimal(10, 2)) + 0.90,
    1950 + i.n % 76,
    @firstAuthorId + i.n % 4000,
    1 + i.n % 4
FROM isbn12 i
JOIN @adj a ON a.n = i.n % 30
JOIN @noun o ON o.n = (i.n / 30) % 40;

-- Plenty of stock: load tests must not fail on "out of stock". The 20 classics only have 25 copies
-- (Don Quixote none), so top them up too; checkout tests would otherwise get 409 after a few minutes.
UPDATE inventory.StockItems SET QuantityOnHand = 1000000 WHERE QuantityOnHand < 1000000;

INSERT inventory.StockItems (BookId, QuantityOnHand, QuantityReserved)
SELECT b.Id, 1000000, 0
FROM catalog.Books b
WHERE NOT EXISTS (SELECT 1 FROM inventory.StockItems s WHERE s.BookId = b.Id);

COMMIT;

SELECT COUNT(*) AS Books FROM catalog.Books;
