using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Identity;

// Users and their password hashes; kept in its own SQLite file next to the catalog database.
public sealed class AuthContext(DbContextOptions<AuthContext> options) : IdentityDbContext<IdentityUser>(options);
