using Bookstore.Institutional;
using Microsoft.EntityFrameworkCore;

static class OptionA
{
    public static async Task<bool> TryAddUserAsync(BookstoreContext db, InstitutionUser user)
    {
        // No unique index; the check lives in the application
        bool taken = await db.InstitutionUsers.AnyAsync(u => u.Email == user.Email && !u.IsDeleted);
        if (taken)
            return false;

        // If two requests get here at the same time, both have seen "free"
        db.InstitutionUsers.Add(user);
        await db.SaveChangesAsync();
        return true;
    }
}
