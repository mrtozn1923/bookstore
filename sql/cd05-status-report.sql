SET NOCOUNT ON;

SELECT StatusId, COUNT(*) AS Orders, SUM(TotalAmount) AS Total
FROM dbo.Orders
GROUP BY StatusId;

SELECT s.Code, s.NameTr, COUNT(*) AS Orders, SUM(o.TotalAmount) AS Total
FROM dbo.Orders o
JOIN dbo.OrderStatuses s ON s.Id = o.StatusId
GROUP BY s.Code, s.NameTr;
