/* Read-only login for the bookstore-db MCP server. Local development databases only.
   sqlcmd -S localhost -i plugins/bookstore-dev/sql/create-readonly-login.sql -v ReadOnlyPassword="..." */
CREATE LOGIN bookstore_readonly WITH PASSWORD = '$(ReadOnlyPassword)';
GO
USE BookstoreDb;
GO
CREATE USER bookstore_readonly FOR LOGIN bookstore_readonly;
ALTER ROLE db_datareader ADD MEMBER bookstore_readonly;
GO
