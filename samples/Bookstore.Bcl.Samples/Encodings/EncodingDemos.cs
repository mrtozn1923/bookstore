using System.Buffers.Text;
using System.Net;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.WebUtilities;

namespace Bookstore.Bcl.Samples.Encodings;

public static class EncodingDemos
{
    public static IEnumerable<(string Name, Action Run)> All =>
    [
        ("encoding-guess", Guess),
        ("encoding-counts", Counts),
        ("encoding-bom", Bom),
        ("encoding-1254", Windows1254),
        ("encoding-strict", Strict),
        ("encoding-base64", Base64Demo),
        ("encoding-hex", Hex),
        ("encoding-url-escape", UrlEscape),
        ("encoding-url-build", UrlBuild),
        ("encoding-uri", UriDemo),
    ];

    private const string Title = "Şeker Portakalı";

    private static void Guess()
    {
        Console.WriteLine(Title.Length);
        Console.WriteLine(Encoding.UTF8.GetByteCount(Title));
        Console.WriteLine(Convert.ToBase64String(Encoding.UTF8.GetBytes("Ada:example-password-change-me")));
    }

    private static void Counts()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding[] encodings = [Encoding.UTF8, Encoding.Unicode, Encoding.Latin1, Encoding.ASCII, Encoding.GetEncoding(1254)];

        Console.WriteLine($"\"{Title}\" has {Title.Length} chars");
        foreach (Encoding encoding in encodings)
        {
            byte[] bytes = encoding.GetBytes(Title);
            string back = encoding.GetString(bytes);
            Console.WriteLine($"{encoding.WebName,-12} {encoding.GetByteCount(Title),3} bytes  {Convert.ToHexString(bytes[..4]),-9}...  round-trip: {back}");
        }
    }

    private static void Bom()
    {
        byte[] preamble = Encoding.UTF8.GetPreamble();
        Console.WriteLine($"Encoding.UTF8 preamble: {Convert.ToHexString(preamble)} ({preamble.Length} bytes)");
        Console.WriteLine($"GetBytes adds BOM: {Encoding.UTF8.GetBytes("A").Length == 4}");

        string path = Path.Combine(Path.GetTempPath(), $"bookstore-orders-{Environment.ProcessId}.csv");
        try
        {
            File.WriteAllText(path, "OrderNumber,Total\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            byte[] fromExcel = File.ReadAllBytes(path);
            Console.WriteLine($"File starts with: {Convert.ToHexString(fromExcel[..6])}");

            string naive = Encoding.UTF8.GetString(fromExcel);
            string header = naive.Split(',')[0];
            Console.WriteLine($"GetString header: \"{header}\" length={header.Length} == \"OrderNumber\": {header == "OrderNumber"}");
            Console.WriteLine($"First char: U+{(int)header[0]:X4}");

            string viaReader = File.ReadAllText(path);
            Console.WriteLine($"File.ReadAllText header == \"OrderNumber\": {viaReader.Split(',')[0] == "OrderNumber"}");

            ReadOnlySpan<byte> body = fromExcel.AsSpan().StartsWith(preamble) ? fromExcel.AsSpan(preamble.Length) : fromExcel;
            Console.WriteLine($"Skip preamble, then GetString: {Encoding.UTF8.GetString(body).Split(',')[0] == "OrderNumber"}");
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void Windows1254()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding turkish = Encoding.GetEncoding(1254);

        // Customer name field exported by the legacy accounting system (Windows-1254).
        byte[] legacy = [0xDE, 0x65, 0x6B, 0x65, 0x72, 0x20, 0x50, 0x6F, 0x72, 0x74, 0x61, 0x6B, 0x61, 0x6C, 0xFD];

        Console.WriteLine($"as 1254  : {turkish.GetString(legacy)}");
        Console.WriteLine($"as UTF-8 : {Encoding.UTF8.GetString(legacy)}");
        Console.WriteLine($"as Latin1: {Encoding.Latin1.GetString(legacy)}");

        byte[] utf8 = Encoding.UTF8.GetBytes(Title);
        Console.WriteLine($"UTF-8 bytes read as 1254: {turkish.GetString(utf8)}");

        byte[] converted = Encoding.Convert(turkish, Encoding.UTF8, legacy);
        Console.WriteLine($"Encoding.Convert 1254 -> UTF-8: {legacy.Length} -> {converted.Length} bytes, {Encoding.UTF8.GetString(converted)}");
    }

    private static void Strict()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        byte[] legacy = Encoding.GetEncoding(1254).GetBytes(Title);

        var strictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        try
        {
            strictUtf8.GetString(legacy);
        }
        catch (DecoderFallbackException ex)
        {
            Console.WriteLine($"{ex.GetType().Name}: {ex.Message.Split('\n')[0]}");
        }

        Encoding strictAscii = Encoding.GetEncoding("us-ascii", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        Console.WriteLine($"ASCII (default fallback): {Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(Title))}");
        try
        {
            strictAscii.GetBytes(Title);
        }
        catch (EncoderFallbackException ex)
        {
            Console.WriteLine($"{ex.GetType().Name}: {ex.Message.Split('\n')[0]}");
        }
    }

    private static void Base64Demo()
    {
        string basic = Convert.ToBase64String(Encoding.UTF8.GetBytes("Ada:example-password-change-me"));
        Console.WriteLine($"Base64 : {basic}");
        Console.WriteLine($"Decoded: {Encoding.UTF8.GetString(Convert.FromBase64String(basic))}");

        byte[] token = [0xFB, 0xEF, 0xFF, 0x3E, 0x3F, 0x00, 0x10];
        string standard = Convert.ToBase64String(token);
        string urlSafe = Base64Url.EncodeToString(token);
        Console.WriteLine($"Base64    : {standard}");
        Console.WriteLine($"Base64Url : {urlSafe}");
        Console.WriteLine($"Round-trip: {Base64Url.DecodeFromChars(urlSafe).AsSpan().SequenceEqual(token)}");
        Console.WriteLine($"Standard Base64 in a query string: {Uri.EscapeDataString(standard)}");

        try
        {
            Convert.FromBase64String(urlSafe);
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"FromBase64String(Base64Url) -> {ex.GetType().Name}: {ex.Message}");
        }

        Console.WriteLine($"Size: {token.Length} bytes -> {standard.Length} chars ({Base64.GetMaxEncodedToUtf8Length(token.Length)} max)");
    }

    private static void Hex()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        byte[] hash = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes("9780441172719"));
        Console.WriteLine($"ToHexString      : {Convert.ToHexString(hash[..8])}");
        Console.WriteLine($"ToHexStringLower : {Convert.ToHexStringLower(hash[..8])}");
        Console.WriteLine($"BitConverter     : {BitConverter.ToString(hash[..8])}");
        Console.WriteLine($"FromHexString    : {Convert.FromHexString("DE656B6572").Length} bytes, {Encoding.GetEncoding(1254).GetString(Convert.FromHexString("DE656B6572"))}");
        Console.WriteLine($"255.ToString(\"x2\"): {255.ToString("x2")}");
    }

    private static void UrlEscape()
    {
        string[] inputs = [Title, "C# in Depth", "Dune & Children of Dune", "50% off/new?"];
        Console.WriteLine($"{"input",-26}{"Uri.EscapeDataString",-50}{"WebUtility.UrlEncode",-46}HttpUtility.UrlEncode");
        foreach (string input in inputs)
            Console.WriteLine($"{input,-26}{Uri.EscapeDataString(input),-50}{WebUtility.UrlEncode(input),-46}{HttpUtility.UrlEncode(input)}");
    }

    private static void UrlBuild()
    {
        const string baseUrl = "https://bookstore.example.com/search";
        string[] queries = [Title, "C# in Depth", "Dune & Children of Dune"];

        foreach (string q in queries)
        {
            string concatenated = $"{baseUrl}?q={q}&lang=tr";
            var parsed = new Uri(concatenated);
            string? qValue = HttpUtility.ParseQueryString(parsed.Query)["q"];
            Console.WriteLine($"concat : {concatenated}");
            Console.WriteLine($"         server sees q=\"{qValue}\", fragment=\"{parsed.Fragment}\"");

            string built = QueryHelpers.AddQueryString(baseUrl, new Dictionary<string, string?> { ["q"] = q, ["lang"] = "tr" });
            string? builtValue = HttpUtility.ParseQueryString(new Uri(built).Query)["q"];
            Console.WriteLine($"helper : {built}");
            Console.WriteLine($"         server sees q=\"{builtValue}\"");
        }
    }

    private static void UriDemo()
    {
        var uri = new Uri("https://bookstore.example.com/search?q=%C5%9Eeker%20Portakal%C4%B1&lang=tr");
        Console.WriteLine($"ToString()     : {uri}");
        Console.WriteLine($"AbsoluteUri    : {uri.AbsoluteUri}");
        Console.WriteLine($"OriginalString : {uri.OriginalString}");
        Console.WriteLine($"Host={uri.Host} Path={uri.AbsolutePath} Query={uri.Query}");

        var relative = new Uri(new Uri("https://bookstore.example.com/books/"), "../authors/ada");
        Console.WriteLine($"Combine        : {relative}");

        Console.WriteLine($"TryCreate(\"bookstore.example.com/books\"): {Uri.TryCreate("bookstore.example.com/books", UriKind.Absolute, out _)}");
        Console.WriteLine($"TryCreate(\"javascript:alert(1)\"): {Uri.TryCreate("javascript:alert(1)", UriKind.Absolute, out Uri? js)} scheme={js?.Scheme}");
        var builder = new UriBuilder("https", "bookstore.example.com")
        {
            Path = "books/kurk-mantolu-madonna",
            Query = $"ref={Uri.EscapeDataString("newsletter 11/2026")}",
        };
        Console.WriteLine($"UriBuilder: {builder.Uri.AbsoluteUri}");
    }
}
