USE master;
GO

IF SUSER_ID(N'bookstore_app') IS NULL
    CREATE LOGIN bookstore_app WITH PASSWORD = N'$(BOOKSTORE_DB_PASSWORD)';
ELSE
    ALTER LOGIN bookstore_app WITH PASSWORD = N'$(BOOKSTORE_DB_PASSWORD)';
GO

USE BookstoreDb;
GO

IF USER_ID(N'bookstore_app') IS NULL
    CREATE USER bookstore_app FOR LOGIN bookstore_app;
ALTER ROLE db_datareader ADD MEMBER bookstore_app;
GO
