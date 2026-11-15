using Microsoft.AspNetCore.Authorization;

namespace Bookstore.Admin.Client.Auth;

// The panel's roles and policies, shared by the server and the WebAssembly side.
// They decide what the UI shows; Bookstore.Api checks the same roles again on every write.
public static class PanelPolicies
{
    public const string StoreStaff = "StoreStaff";
    public const string WarehouseStaff = "WarehouseStaff";
    public const string Admin = "Admin";

    public const string Staff = "Staff";
    public const string ManageCatalog = "ManageCatalog";

    public static void AddPanelPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(Staff, policy => policy.RequireRole(StoreStaff, WarehouseStaff, Admin));
        options.AddPolicy(ManageCatalog, policy => policy.RequireRole(StoreStaff, Admin));
    }
}
