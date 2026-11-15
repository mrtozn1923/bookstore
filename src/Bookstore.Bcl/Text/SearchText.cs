using System.Globalization;
using System.Text;

namespace Bookstore.Bcl.Text;

public static class SearchText
{
    // Folds text for a forgiving catalog search: "İSTANBUL ŞEHİR" -> "istanbul sehir".
    public static string Fold(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        string decomposed = text.Normalize(NormalizationForm.FormD);
        var folded = new StringBuilder(decomposed.Length);
        foreach (char c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            folded.Append(c == 'ı' ? 'i' : char.ToLowerInvariant(c));
        }

        return folded.ToString().Normalize(NormalizationForm.FormC);
    }
}
