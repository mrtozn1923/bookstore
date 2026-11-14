using Microsoft.AspNetCore.Identity;

namespace Bookstore.Api.Auth;

public class AppUser
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
    public DateTime? StaffSince { get; set; }
}

// an in-memory user store for now; in a real project the users live in the database
public class UserStore
{
    private const string DemoPassword = "Pa55word!";

    private readonly List<AppUser> _users = new();
    private readonly PasswordHasher<AppUser> _hasher = new();

    public UserStore()
    {
        Add(new AppUser { Id = 1, Email = "ada@example.com", DisplayName = "Ada Lovelace", Roles = new[] { "Admin" } });
        Add(new AppUser { Id = 2, Email = "alan@example.com", DisplayName = "Alan Turing", Roles = new[] { "StoreStaff" }, StaffSince = new DateTime(2024, 3, 1) });
        Add(new AppUser { Id = 3, Email = "grace@example.com", DisplayName = "Grace Hopper", Roles = new[] { "StoreStaff" }, StaffSince = new DateTime(2026, 10, 31) });
        Add(new AppUser { Id = 4, Email = "margaret@example.com", DisplayName = "Margaret Hamilton", Roles = new[] { "Customer" } });
        Add(new AppUser { Id = 5, Email = "linus@example.com", DisplayName = "Linus Torvalds", Roles = new[] { "Customer" } });
    }

    public AppUser? ValidateCredentials(string email, string password)
    {
        var user = _users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        if (user is null)
        {
            return null;
        }

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Failed ? null : user;
    }

    private void Add(AppUser user)
    {
        // the password is never stored as plain text
        user.PasswordHash = _hasher.HashPassword(user, DemoPassword);
        _users.Add(user);
    }
}
