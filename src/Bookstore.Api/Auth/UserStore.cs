using Microsoft.AspNetCore.Identity;

namespace Bookstore.Api.Auth;

public class AppUser
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}

// an in-memory user store for now; in a real project the users live in the database
public class UserStore
{
    private readonly List<AppUser> _users = new();
    private readonly PasswordHasher<AppUser> _hasher = new();

    public UserStore()
    {
        Add(new AppUser { Id = 1, Email = "ada@example.com", DisplayName = "Ada Lovelace" }, "Pa55word!");
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

    private void Add(AppUser user, string password)
    {
        // the password is never stored as plain text
        user.PasswordHash = _hasher.HashPassword(user, password);
        _users.Add(user);
    }
}
