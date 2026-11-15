CREATE TABLE dbo.DailySalesReport
(
    ReportDate date NOT NULL,
    Category varchar(30) NOT NULL,
    Orders int NOT NULL,
    Items int NOT NULL,
    NetRevenue decimal(14,2) NOT NULL,
    CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_DailySalesReport_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_DailySalesReport PRIMARY KEY (ReportDate, Category)
);
