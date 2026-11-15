INSERT dbo.DailySalesReport (ReportDate, Category, Orders, Items, Revenue)
VALUES ('2026-10-19', 'Classic', 565, 1112, 31131.85), ('2026-10-19', 'Fantasy', 146, 192, 5339.03),
       ('2026-10-19', 'Horror', 70, 85, 2554.59), ('2026-10-19', 'SciFi', 382, 578, 16115.50);
INSERT dbo.SalesTarget (TargetMonth, Category, TargetRevenue)
VALUES ('2026-10-01', 'Classic', 900000), ('2026-10-01', 'SciFi', 450000);
SELECT * FROM dbo.MonthlySalesVsTarget;
