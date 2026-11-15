using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace Bookstore.Web.Ordering;

// Anonymous cart in the browser: "bookId:quantity" pairs, encrypted and signed with Data Protection.
public sealed class CookieCartStore(IDataProtectionProvider provider, ILogger<CookieCartStore> logger) : ICartStore
{
    public const string CookieName = "bookstore_cart";
    private readonly IDataProtector protector = provider.CreateProtector("Bookstore.Web.Cart.v1");

    public IReadOnlyList<CartLine> Read(HttpContext context)
    {
        string? value = context.Request.Cookies[CookieName];
        if (string.IsNullOrEmpty(value))
        {
            return [];
        }

        try
        {
            return protector.Unprotect(value)
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(pair => pair.Split(':'))
                .Select(parts => new CartLine(int.Parse(parts[0]), int.Parse(parts[1])))
                .ToList();
        }
        catch (CryptographicException ex)
        {
            // Tampered cookie or a key this instance doesn't have: start with an empty cart
            logger.LogWarning(ex, "Cart cookie could not be read");
            return [];
        }
    }

    public void Write(HttpContext context, IReadOnlyList<CartLine> lines)
    {
        string payload = string.Join(',', lines.Select(l => $"{l.BookId}:{l.Quantity}"));
        context.Response.Cookies.Append(CookieName, protector.Protect(payload), new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(30),
            IsEssential = true // the cart is what the user asked for
        });
    }
}
