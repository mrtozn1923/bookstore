/* Generated test data with the DiscountAgreements schema: 180,000 rows, about 2% open-ended */
CREATE TABLE dbo.AgreementPerf
(
    Id               int IDENTITY(1, 1) PRIMARY KEY,
    InstitutionId    int  NOT NULL,
    StartDate        date NOT NULL,
    EndDate          date NULL,
    EffectiveEndDate AS ISNULL(EndDate, DATEFROMPARTS(9999, 12, 31)) PERSISTED
);

WITH n AS (
    SELECT TOP (180000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i
    FROM sys.all_objects a CROSS JOIN sys.all_objects b
)
INSERT dbo.AgreementPerf (InstitutionId, StartDate, EndDate)
SELECT i % 120 + 1,
       DATEADD(day, -(i % 3650), '2026-11-01'),
       CASE WHEN i % 50 = 0 THEN NULL
            ELSE DATEADD(day, (i * 7) % 60 + 1, DATEADD(day, -(i % 3650), '2026-11-01')) END
FROM n;

CREATE INDEX IX_EndDate ON dbo.AgreementPerf (EndDate) INCLUDE (StartDate);
CREATE INDEX IX_EffectiveEndDate ON dbo.AgreementPerf (EffectiveEndDate) INCLUDE (StartDate);
GO
SET STATISTICS IO ON;
DECLARE @d date = '2026-10-30';
SELECT COUNT(*) FROM dbo.AgreementPerf WITH (INDEX(IX_EndDate))
WHERE StartDate <= @d AND (EndDate IS NULL OR EndDate >= @d);
SELECT COUNT(*) FROM dbo.AgreementPerf
WHERE StartDate <= @d AND EffectiveEndDate >= @d;
SET STATISTICS IO OFF;
GO
