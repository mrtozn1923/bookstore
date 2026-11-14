-- Run in BookstoreDb as a member of sg-bookstore-dba (the Entra admin group), e.g. from VS Code's MSSQL extension.
-- The name is the managed identity's display name; Azure SQL looks it up in Microsoft Entra ID.
CREATE USER [id-bookstore-dev-weu] FROM EXTERNAL PROVIDER;

-- The API reads and writes data; it doesn't change the schema
ALTER ROLE db_datareader ADD MEMBER [id-bookstore-dev-weu];
ALTER ROLE db_datawriter ADD MEMBER [id-bookstore-dev-weu];

-- Migrations run from the deployment pipeline with a separate identity that may change the schema
CREATE USER [id-bookstore-deploy-dev-weu] FROM EXTERNAL PROVIDER;
ALTER ROLE db_ddladmin ADD MEMBER [id-bookstore-deploy-dev-weu];
ALTER ROLE db_datareader ADD MEMBER [id-bookstore-deploy-dev-weu];
ALTER ROLE db_datawriter ADD MEMBER [id-bookstore-deploy-dev-weu];
