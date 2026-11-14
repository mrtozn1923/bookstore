-- Post 21: 2 million institutional orders (run in an empty database)
CREATE TABLE dbo.Orders
(
    Id int IDENTITY PRIMARY KEY,
    InstitutionId int NOT NULL,
    InstitutionUnitId int NOT NULL,
    CreatedAt datetime2(0) NOT NULL,
    ApprovedAt datetime2(0) NULL,
    TotalAmount decimal(19, 4) NOT NULL,
    Status tinyint NOT NULL
);
/* 2 million rows, 5 institutions; one second per 10 rows so that several orders share a second */
INSERT dbo.Orders (InstitutionId, InstitutionUnitId, CreatedAt, ApprovedAt, TotalAmount, Status)
SELECT s.value % 5 + 5,
       s.value % 300 + 1,
       DATEADD(second, s.value / 10, '2024-01-01'),
       CASE WHEN s.value % 4 = 0 THEN NULL ELSE DATEADD(second, s.value / 10 + 3600, '2024-01-01') END,
       CAST(s.value % 9000 + 100 AS decimal(19, 4)),
       s.value % 4
FROM GENERATE_SERIES(1, 2000000) AS s;
GO
