-- Row-level security for the shared Bookstore database (run after the tables exist).
-- Invoices is left out on purpose: the isolation checks must find it.
CREATE SCHEMA Security;
GO
CREATE FUNCTION Security.fn_TenantPredicate(@InstitutionId int)
    RETURNS TABLE
    WITH SCHEMABINDING
AS
    RETURN SELECT 1 AS Allowed
    WHERE @InstitutionId = CAST(SESSION_CONTEXT(N'TenantId') AS int);
GO
CREATE SECURITY POLICY Security.TenantIsolation
    ADD FILTER PREDICATE Security.fn_TenantPredicate(InstitutionId) ON dbo.InstitutionUsers,
    ADD BLOCK PREDICATE Security.fn_TenantPredicate(InstitutionId) ON dbo.InstitutionUsers AFTER INSERT,
    ADD BLOCK PREDICATE Security.fn_TenantPredicate(InstitutionId) ON dbo.InstitutionUsers AFTER UPDATE,
    ADD FILTER PREDICATE Security.fn_TenantPredicate(InstitutionId) ON dbo.PurchaseApprovals,
    ADD BLOCK PREDICATE Security.fn_TenantPredicate(InstitutionId) ON dbo.PurchaseApprovals AFTER INSERT,
    ADD BLOCK PREDICATE Security.fn_TenantPredicate(InstitutionId) ON dbo.PurchaseApprovals AFTER UPDATE
    WITH (STATE = ON);
GO
