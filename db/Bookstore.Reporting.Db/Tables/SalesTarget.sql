CREATE TABLE dbo.SalesTarget
(
    TargetMonth date NOT NULL,
    Category varchar(30) NOT NULL,
    TargetRevenue decimal(14,2) NOT NULL,
    CONSTRAINT PK_SalesTarget PRIMARY KEY (TargetMonth, Category),
    CONSTRAINT CK_SalesTarget_FirstDay CHECK (DATEPART(DAY, [TargetMonth]) = (1))
);
