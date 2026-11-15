ALTER TABLE dbo.PriceHistory
    ADD CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_PriceHistory_CreatedAt DEFAULT SYSUTCDATETIME();
