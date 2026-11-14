/* Opening: values migrated from an old system with "reasonable" sentinels */
CREATE TABLE dbo.InstitutionUsers
(
    Id            int IDENTITY(1, 1) PRIMARY KEY,
    FullName      nvarchar(200) NOT NULL,
    SpendingLimit decimal(19, 4) NOT NULL  /* 0 = not decided yet */
);

CREATE TABLE dbo.DiscountAgreements
(
    Id            int IDENTITY(1, 1) PRIMARY KEY,
    InstitutionId int  NOT NULL,
    StartDate     date NOT NULL,
    EndDate       date NOT NULL  /* 9999-12-31 = open-ended */
);

INSERT dbo.InstitutionUsers (FullName, SpendingLimit) VALUES
    (N'Ada Lovelace', 85000), (N'Alan Turing', 92000), (N'Grace Hopper', 78000),
    (N'Linus Torvalds', 0), (N'Barbara Liskov', 0);

INSERT dbo.DiscountAgreements (InstitutionId, StartDate, EndDate) VALUES
    (1, '2025-09-01', '2026-02-28'),
    (2, '2025-10-01', '2026-03-31'),
    (3, '2026-03-23', '9999-12-31');
GO
SELECT COUNT(*) AS Users, AVG(SpendingLimit) AS AvgLimit, MIN(SpendingLimit) AS MinLimit
FROM dbo.InstitutionUsers;

SELECT SUM(DATEDIFF(day, StartDate, EndDate) + 1) AS AgreementDays
FROM dbo.DiscountAgreements;
GO
SELECT CAST('' AS datetime) AS FromEmptyString;
SELECT CAST(0 AS datetime) AS FromZero;
SELECT CAST('' AS date) AS EmptyToDate;
GO
/* Option A: nullable column and three-valued logic */
ALTER TABLE dbo.DiscountAgreements ALTER COLUMN EndDate date NULL;
GO
UPDATE dbo.DiscountAgreements SET EndDate = NULL WHERE EndDate = '9999-12-31';

SELECT COUNT(*) AS NotEndingFeb28
FROM dbo.DiscountAgreements
WHERE EndDate <> '2026-02-28';
GO
