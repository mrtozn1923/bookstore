CREATE TABLE dbo.PriceHistory
(
    PriceHistoryId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_PriceHistory PRIMARY KEY,
    Isbn varchar(13) NOT NULL,
    Price decimal(10,2) NOT NULL,
    Currency char(3) NOT NULL,
    ValidFrom date NOT NULL,
    Source varchar(50) NOT NULL
);
