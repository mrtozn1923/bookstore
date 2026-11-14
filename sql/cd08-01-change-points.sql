-- Post 08: store only change points, derive the periods with LEAD
CREATE TABLE dbo.DiscountChanges
(
    InstitutionId int          NOT NULL,
    EffectiveFrom date         NOT NULL,
    DiscountRate  decimal(5,4) NOT NULL,
    CONSTRAINT PK_DiscountChanges PRIMARY KEY (InstitutionId, EffectiveFrom)
);
GO
CREATE VIEW dbo.DiscountPeriods AS
SELECT InstitutionId, DiscountRate,
       EffectiveFrom AS ValidFrom,
       LEAD(EffectiveFrom, 1, '9999-12-31') OVER (PARTITION BY InstitutionId ORDER BY EffectiveFrom) AS ValidTo
FROM dbo.DiscountChanges;
GO
INSERT dbo.DiscountChanges VALUES (1, '2026-01-01', 0.10), (1, '2026-03-01', 0.12);
SELECT InstitutionId, DiscountRate, ValidFrom, ValidTo FROM dbo.DiscountPeriods ORDER BY ValidFrom;
