SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @batch_size INT = $(BatchSize), @batch_no INT = 0, @from_id INT, @to_id INT = 0, @rows INT, @msg NVARCHAR(2048);
WHILE 1 = 1
BEGIN
    SET @from_id = @to_id + 1;
    SELECT @to_id = MAX(book_id)
    FROM (SELECT TOP (@batch_size) book_id FROM dbo.price_updates WHERE book_id >= @from_id ORDER BY book_id) AS next_batch;
    IF @to_id IS NULL BREAK;
    SET @batch_no += 1;
    BEGIN TRY
        BEGIN TRAN;
        UPDATE b
        SET list_price = p.new_price
        FROM dbo.books AS b
        JOIN dbo.price_updates AS p ON p.book_id = b.book_id
        WHERE b.book_id BETWEEN @from_id AND @to_id
          AND b.list_price <> p.new_price;
        SET @rows = @@ROWCOUNT;
        COMMIT;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        SET @msg = CONCAT(N'Batch ', @batch_no, N' (book_id ', @from_id, N'-', @to_id, N') failed: ', ERROR_MESSAGE());
        THROW 50010, @msg, 1;
    END CATCH;
    RAISERROR(N'Batch %d: book_id %d-%d, %d rows updated', 0, 1, @batch_no, @from_id, @to_id, @rows) WITH NOWAIT;
END;
