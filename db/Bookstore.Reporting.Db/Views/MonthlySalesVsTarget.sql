CREATE VIEW dbo.MonthlySalesVsTarget
AS
SELECT t.TargetMonth, t.Category, t.TargetRevenue,
       ISNULL(SUM(r.NetRevenue), 0) AS Revenue
FROM dbo.SalesTarget AS t
LEFT JOIN dbo.DailySalesReport AS r
    ON r.Category = t.Category
   AND r.ReportDate >= t.TargetMonth
   AND r.ReportDate < DATEADD(MONTH, 1, t.TargetMonth)
GROUP BY t.TargetMonth, t.Category, t.TargetRevenue;
