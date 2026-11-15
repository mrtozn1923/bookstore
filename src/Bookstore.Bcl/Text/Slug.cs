using System.Globalization;
using System.Text;

namespace Bookstore.Bcl.Text;

public static class Slug
{
    // Builds a URL slug from a book title: "Kürk Mantolu Madonna" -> "kurk-mantolu-madonna".
    public static string FromTitle(string title, int maxLength = 80)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        // FormD splits "ü" into "u" + U+0308 (combining diaeresis); the mark is dropped below.
        string decomposed = title.Normalize(NormalizationForm.FormD);
        var slug = new StringBuilder(Math.Min(decomposed.Length, maxLength));
        bool pendingDash = false;

        foreach (char c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;

            // Dotless i has no decomposition, so it is mapped by hand.
            char lower = c == 'ı' ? 'i' : char.ToLowerInvariant(c);

            if (char.IsAsciiLetterOrDigit(lower))
            {
                int needed = pendingDash && slug.Length > 0 ? 2 : 1;
                if (slug.Length + needed > maxLength)
                    break;
                if (needed == 2)
                    slug.Append('-');
                slug.Append(lower);
                pendingDash = false;
            }
            else
            {
                pendingDash = true;
            }
        }

        return slug.ToString();
    }
}
