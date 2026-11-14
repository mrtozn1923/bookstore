CREATE TABLE dbo.Suppliers
(
    Id        int IDENTITY PRIMARY KEY,
    TaxNumber varchar(10)   NOT NULL UNIQUE,
    Name      nvarchar(200) NOT NULL
);
INSERT dbo.Suppliers (TaxNumber, Name) VALUES ('1234567890', N'Acme Book Distribution'), ('9876543210', N'Sample Book Wholesale');
GO
CREATE TABLE dbo.DeletedRecords
(
    Id            bigint IDENTITY PRIMARY KEY,
    OriginalTable sysname      NOT NULL,
    OriginalId    int          NOT NULL,
    DeletedAt     datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
    DeletedBy     nvarchar(100) NULL,
    Data          json         NOT NULL
);
GO
CREATE TRIGGER dbo.TR_Suppliers_Archive ON dbo.Suppliers AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;
    INSERT dbo.DeletedRecords (OriginalTable, OriginalId, DeletedBy, Data)
    SELECT N'Suppliers', d.Id,
           CAST(SESSION_CONTEXT(N'UserName') AS nvarchar(100)),
           (SELECT d.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
    FROM deleted AS d;
END;
GO
EXEC sys.sp_set_session_context @key = N'UserName', @value = N'grace';
DELETE dbo.Suppliers WHERE TaxNumber = '1234567890';
SELECT OriginalTable, OriginalId, DeletedBy, CONVERT(nvarchar(max), Data) AS Data FROM dbo.DeletedRecords;
