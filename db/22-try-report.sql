/* Manual test of the report procedure */
EXEC dbo.FillDailySalesReport @ReportDate = '2026-10-19';
EXEC dbo.FillDailySalesReport @ReportDate = '2026-10-19';
SELECT Category, Orders, Items, Revenue FROM dbo.DailySalesReport WHERE ReportDate = '2026-10-19' ORDER BY Category;
EXEC dbo.FillDailySalesReport;
