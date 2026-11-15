using System.Globalization;
using System.Text;

namespace Bookstore.Web.Urls;

// Teknik SEO 01: Türkçe başlıktan ASCII slug
public static class Slugifier
{
    // Unicode normalization ile ayrışmayan karakterler
    private static readonly Dictionary<char, string> Special = new()
    {
        ['ı'] = "i", ['İ'] = "i", ['ß'] = "ss", ['æ'] = "ae", ['ø'] = "o", ['đ'] = "d", ['ł'] = "l",
    };

    public static string ToSlug(string title, int maxLength = 80)
    {
        string text = title
            .Replace("C#", "csharp", StringComparison.OrdinalIgnoreCase)
            .Replace(".NET", "net", StringComparison.OrdinalIgnoreCase);

        StringBuilder sb = new(text.Length);
        foreach (char c in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue; // birleşik işaretleri at: c + çengel -> c
            if (Special.TryGetValue(c, out string? mapped))
                sb.Append(mapped);
            else if (char.IsAsciiLetterOrDigit(c))
                sb.Append(char.ToLowerInvariant(c));
            else if (c is '\'' or '’')
                continue;
            else
                sb.Append('-');
        }

        string slug = string.Join('-', sb.ToString().Split('-', StringSplitOptions.RemoveEmptyEntries));
        if (slug.Length <= maxLength)
            return slug;

        int cut = slug.LastIndexOf('-', maxLength);
        return cut > 0 ? slug[..cut] : slug[..maxLength];
    }
}
