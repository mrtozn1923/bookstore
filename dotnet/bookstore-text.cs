using System.Text;
using System.Text.RegularExpressions;

foreach (string isbn in new[] { "978-0-441-17271-9", "978-0-441-17271-8", "978 0 441 17271 9" })
    Console.WriteLine($"{isbn,-20} {BookstoreText.IsIsbn13(isbn)}");

foreach (string title in new[] { "Çalıkuşu", "İnce Memed: Cilt 1", "Robinson Crusoe: Issız Bir Adada 28 Yıl", "  Dune -- 2. Baskı  " })
    Console.WriteLine($"{title,-42} {BookstoreText.Slugify(title)}");

static partial class BookstoreText
{
    // .NET 9 and later: a partial property
    [GeneratedRegex(@"^97[89]-[0-9]{1,5}-[0-9]{1,7}-[0-9]{1,7}-[0-9]\z")]
    private static partial Regex Isbn13Shape { get; }

    // .NET 7 and later: a partial method
    [GeneratedRegex(@"\p{Mn}")]
    private static partial Regex CombiningMarks();

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonSlugCharacters { get; }

    public static bool IsIsbn13(string input)
    {
        // regex checks the shape, code checks the length and the check digit
        if (input.Length != 17 || !Isbn13Shape.IsMatch(input))
            return false;
        string digits = input.Replace("-", "");
        int sum = 0;
        for (int i = 0; i < 12; i++)
            sum += (digits[i] - '0') * (i % 2 == 0 ? 1 : 3);
        return (10 - sum % 10) % 10 == digits[12] - '0';
    }

    public static string Slugify(string title)
    {
        // FormD splits ç into c + combining cedilla; dotless ı has no decomposition, so map it first
        string decomposed = title.Replace('ı', 'i').Normalize(NormalizationForm.FormD);
        string ascii = CombiningMarks().Replace(decomposed, "").ToLowerInvariant();
        return NonSlugCharacters.Replace(ascii, "-").Trim('-');
    }
}
