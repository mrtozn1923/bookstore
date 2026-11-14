:on error exit
:setvar BatchSize 20
UPDATE dbo.price_updates SET new_price = 345.00 WHERE book_id = 50;
GO
:r price-update.sql
GO
SELECT COUNT(*) AS updated FROM dbo.books AS b JOIN dbo.price_updates AS p ON p.book_id = b.book_id WHERE b.list_price = p.new_price;
