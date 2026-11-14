-- Option C: one typed column per setting
CREATE TABLE dbo.InstitutionSettings
(
    InstitutionId int NOT NULL PRIMARY KEY,
    InvoiceDueDays int NOT NULL CONSTRAINT DF_InstitutionSettings_InvoiceDueDays DEFAULT (30)
        CONSTRAINT CK_InstitutionSettings_InvoiceDueDays CHECK (InvoiceDueDays BETWEEN 0 AND 180),
    EmailNotifications bit NOT NULL CONSTRAINT DF_InstitutionSettings_EmailNotifications DEFAULT (1)
);
INSERT dbo.InstitutionSettings (InstitutionId) VALUES (2);
SELECT InstitutionId, InvoiceDueDays, EmailNotifications FROM dbo.InstitutionSettings;
GO
-- Option D: one json document per institution (SQL Server 2025 json type)
CREATE TABLE dbo.InstitutionSettingsDoc
(
    InstitutionId int NOT NULL PRIMARY KEY,
    Settings json NOT NULL
);
INSERT dbo.InstitutionSettingsDoc VALUES (2, '{"invoiceDueDays":45,"emailNotifications":true}');
UPDATE dbo.InstitutionSettingsDoc SET Settings.modify('$.invoiceDueDays', 60) WHERE InstitutionId = 2;
SELECT CAST(Settings AS nvarchar(max)) AS Settings FROM dbo.InstitutionSettingsDoc;
UPDATE dbo.InstitutionSettingsDoc SET Settings.modify('$.invoiceDueDays', '60 days') WHERE InstitutionId = 2;
SELECT CAST(Settings AS nvarchar(max)) AS Settings FROM dbo.InstitutionSettingsDoc;
GO
