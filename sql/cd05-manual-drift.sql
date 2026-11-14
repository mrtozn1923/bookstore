SET NOCOUNT ON;
-- someone edits the lookup table by hand
INSERT dbo.OrderStatuses (Id, Code, NameTr, NameEn) VALUES (6, N'OnHold', N'Beklemede', N'On hold');
DELETE FROM dbo.OrderStatuses WHERE Id = 5;
