-- Post 01: what LIKE can and cannot do. Run against the database filled by "seed-sql".
SET NOCOUNT ON;
SET STATISTICS IO ON;

PRINT '--- 1: prefix';
SELECT BookId, Title FROM dbo.Books WHERE Title LIKE N'İnce%';

PRINT '--- 2: prefix, lowercase';
SELECT BookId, Title FROM dbo.Books WHERE Title LIKE N'ince%';

PRINT '--- 3: contains';
SELECT BookId, Title FROM dbo.Books WHERE Title LIKE N'%memed%';

PRINT '--- 4: without Turkish characters';
SELECT BookId, Title FROM dbo.Books WHERE Title LIKE N'%kurk mantolu%';

PRINT '--- 5: words in a different order';
SELECT BookId, Title FROM dbo.Books WHERE Title LIKE N'%memed ince%';

PRINT '--- 6: a different word form';
SELECT BookId, Title FROM dbo.Books WHERE Description LIKE N'%öğretmenler%';

SET STATISTICS IO OFF;

PRINT '--- 7: the same predicates under other collations';
SELECT
    (SELECT COUNT(*) FROM dbo.Books WHERE Title COLLATE Turkish_100_CI_AS LIKE N'ince%') AS TurkishCiAs_ince,
    (SELECT COUNT(*) FROM dbo.Books WHERE Title COLLATE Turkish_100_CI_AI LIKE N'%kurk mantolu%') AS TurkishCiAi_kurk,
    (SELECT COUNT(*) FROM dbo.Books WHERE Title COLLATE Latin1_General_100_CI_AI LIKE N'%kurk mantolu%') AS Latin1CiAi_kurk;
