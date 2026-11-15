using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Admin.Identity;

// Staff accounts of the panel: ASP.NET Core Identity's tables in BookstoreDb, in their own "identity" schema.
public sealed class AdminIdentityContext(DbContextOptions<AdminIdentityContext> options)
    : IdentityDbContext<IdentityUser>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("identity");
    }
}
