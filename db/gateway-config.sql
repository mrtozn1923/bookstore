IF DB_ID(N'BookstoreGatewayDb') IS NULL
    CREATE DATABASE BookstoreGatewayDb;
GO
USE BookstoreGatewayDb;
GO
IF OBJECT_ID(N'dbo.GatewayRoutes') IS NULL
CREATE TABLE dbo.GatewayRoutes
(
    RouteId      nvarchar(100) NOT NULL PRIMARY KEY,
    ClusterId    nvarchar(100) NOT NULL,
    Path         nvarchar(400) NOT NULL,
    [Order]      int           NULL,
    PathPattern  nvarchar(400) NULL,
    IsEnabled    bit           NOT NULL DEFAULT 1
);
GO
