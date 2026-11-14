-- Run in any database: a read-only key cannot be changed on the same session
EXEC sp_set_session_context @key = N'TenantId', @value = 1, @read_only = 1;
SELECT CAST(SESSION_CONTEXT(N'TenantId') AS int) AS TenantId;
EXEC sp_set_session_context @key = N'TenantId', @value = 2;
