using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Api;

public class BookstoreContext(DbContextOptions<BookstoreContext> options) : IdentityDbContext<IdentityUser>(options)
{
}
