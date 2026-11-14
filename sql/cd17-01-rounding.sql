-- Run in any database: ROUND and the decimal conversion round half away from zero
SELECT ROUND(11.225, 2) AS RoundHalf, CAST(11.225 AS decimal(18,2)) AS Half;
GO
-- money truncates intermediate results to 4 decimal places
DECLARE @m money = 10, @d decimal(19,4) = 10;
SELECT @m / 3 * 3 AS MoneyResult, @d / 3 * 3 AS DecimalResult, @d / 3 AS DecimalDiv;
GO
