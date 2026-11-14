CREATE TABLE dbo.SpendingLimitHistory
(
    InstitutionUserId int            NOT NULL PRIMARY KEY,
    SpendingLimit     decimal(19, 4) NULL,
    ValidFrom         datetime2 GENERATED ALWAYS AS ROW START NOT NULL,
    ValidTo           datetime2 GENERATED ALWAYS AS ROW END NOT NULL,
    PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo)
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.SpendingLimitHistoryLog));

INSERT dbo.SpendingLimitHistory (InstitutionUserId, SpendingLimit) VALUES (1, 85000);

SELECT InstitutionUserId, ValidTo FROM dbo.SpendingLimitHistory;
GO
