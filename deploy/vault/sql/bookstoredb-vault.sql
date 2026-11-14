-- BookstoreDb ve Vault database secrets engine'in kullandığı en az yetkili login.
-- Çalıştırma: sqlcmd -S localhost,14350 -U sa -P "$SA_PASSWORD" -C -v VaultAdminPassword="..." -i bookstoredb-vault.sql
CREATE DATABASE BookstoreDb;
GO
USE BookstoreDb;
CREATE TABLE dbo.Books (
    Isbn  char(13)      NOT NULL PRIMARY KEY,
    Title nvarchar(200) NOT NULL,
    Price decimal(10,2) NOT NULL
);
INSERT dbo.Books (Isbn, Title, Price) VALUES
    ('9780441172719', N'Dune', 420.00),
    ('9780141439587', N'Emma', 180.00),
    ('9780547928227', N'The Hobbit', 310.00),
    ('9780141439549', N'Middlemarch', 260.00);
GO
-- Uygulama kullanıcılarının yetkisi bu rolde; Vault yalnızca üye ekler
USE BookstoreDb;
CREATE ROLE bookstore_api;
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO bookstore_api;
GO
USE master;
CREATE LOGIN vault_admin WITH PASSWORD = N'$(VaultAdminPassword)';
GRANT ALTER ANY LOGIN TO vault_admin;               -- login oluşturma/silme, rotate-root
GRANT ALTER ANY CONNECTION TO vault_admin;          -- revoke'ta açık oturumları KILL ile kapatma
GRANT VIEW SERVER PERFORMANCE STATE TO vault_admin; -- sys.dm_exec_sessions'ta başka login'lerin oturumları
GO
USE BookstoreDb;
CREATE USER vault_admin FOR LOGIN vault_admin;
GRANT ALTER ANY USER TO vault_admin;                -- BookstoreDb'de user oluşturma/silme
GRANT ALTER ON ROLE::bookstore_api TO vault_admin;  -- yalnız bu role üye ekleme
GO
