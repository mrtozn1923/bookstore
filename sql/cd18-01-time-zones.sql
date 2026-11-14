-- Run in an empty database
SET NOCOUNT ON;
CREATE TABLE dbo.Orders (Id int NOT NULL PRIMARY KEY, PlacedBy nvarchar(50) NOT NULL, CreatedAtUtc datetime2(0) NOT NULL);
INSERT dbo.Orders VALUES (1, N'Ada', '2026-11-05T21:10:00'), (2, N'Alan', '2026-11-06T09:45:00'), (3, N'Grace', '2026-11-06T22:30:00');
GO
SELECT GETDATE() AS ServerLocal, SYSUTCDATETIME() AS Utc, SYSDATETIMEOFFSET() AS WithOffset;

SELECT PlacedBy,
       CreatedAtUtc AT TIME ZONE 'UTC' AT TIME ZONE 'Turkey Standard Time' AS CreatedAtIstanbul
FROM dbo.Orders;

DECLARE @StartUtc datetime2(0) = '2026-11-05T21:00:00',
        @EndUtc datetime2(0) = '2026-11-06T21:00:00';
SELECT PlacedBy FROM dbo.Orders
WHERE CreatedAtUtc >= @StartUtc AND CreatedAtUtc < @EndUtc;
GO
SELECT CAST('2026-03-29T02:30:00' AS datetime2(0)) AT TIME ZONE 'W. Europe Standard Time' AS InGap,
       CAST('2026-10-25T02:30:00' AS datetime2(0)) AT TIME ZONE 'W. Europe Standard Time' AS Ambiguous;

SELECT * FROM dbo.Orders WHERE CreatedAtUtc AT TIME ZONE 'Europe/Istanbul' > '2026-01-01';
GO
