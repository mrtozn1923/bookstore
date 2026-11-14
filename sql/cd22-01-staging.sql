CREATE TABLE dbo.InstitutionUserImportRows
(
    ImportId uniqueidentifier NOT NULL,
    RowNumber int NOT NULL,
    StaffNumber nvarchar(50) NULL,
    FullName nvarchar(200) NULL,
    Email nvarchar(200) NULL,
    UnitCode nvarchar(50) NULL,
    JoinedOn nvarchar(50) NULL,
    SpendingLimit nvarchar(50) NULL,
    Errors nvarchar(1000) NULL,
    CONSTRAINT PK_InstitutionUserImportRows PRIMARY KEY (ImportId, RowNumber)
);
