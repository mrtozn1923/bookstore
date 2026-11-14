-- Run in the database created by samples/Cd07.AuditTrail (needs dbo.AuditEntries)
CREATE USER bookstore_app WITHOUT LOGIN;
GRANT SELECT, INSERT ON dbo.AuditEntries TO bookstore_app;
DENY UPDATE, DELETE ON dbo.AuditEntries TO bookstore_app;
GO
EXECUTE AS USER = 'bookstore_app';
UPDATE dbo.AuditEntries SET UserName = N'nobody' WHERE Id = 3;
REVERT;
GO
CREATE TABLE dbo.AuditLedger
(
    Id         bigint IDENTITY NOT NULL,
    EntityType nvarchar(100)  NOT NULL,
    EntityId   nvarchar(100)  NOT NULL,
    Changes    nvarchar(max)  NOT NULL,
    UserName   nvarchar(100)  NOT NULL,
    ChangedAt  datetimeoffset NOT NULL
)
WITH (LEDGER = ON (APPEND_ONLY = ON));
GO
INSERT dbo.AuditLedger (EntityType, EntityId, Changes, UserName, ChangedAt)
VALUES (N'Invoice', N'1', N'[{"Property":"TotalAmount","Old":48000.00,"New":4800}]', N'alan', '2026-04-14T09:30:00+00:00');
GO
UPDATE dbo.AuditLedger SET UserName = N'nobody';
GO
DELETE dbo.AuditLedger;
GO
