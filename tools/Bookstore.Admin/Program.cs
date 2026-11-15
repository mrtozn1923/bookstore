using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using Bookstore.Contracts;

return args switch
{
    ["info"] => PrintInfo(),
    ["isbn", string input] => CheckIsbn(input),
    ["price", string amount] => FormatPrice(amount),
    _ => PrintUsage(),
};

static int PrintInfo()
{
    Console.WriteLine($"Runtime:     {RuntimeInformation.FrameworkDescription}");
    Console.WriteLine($"Environment: {Environment.GetEnvironmentVariable("BOOKSTORE_ENVIRONMENT") ?? "(not set)"}");
    Console.WriteLine($"API:         {ReadApiBaseUrl() ?? "(appsettings.json not found)"}");
    return 0;
}

static string? ReadApiBaseUrl()
{
    string path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    if (!File.Exists(path))
    {
        return null;
    }

    using JsonDocument settings = JsonDocument.Parse(File.ReadAllText(path));
    return settings.RootElement.GetProperty("Bookstore").GetProperty("ApiBaseUrl").GetString();
}

static int CheckIsbn(string input)
{
    if (Isbn.TryParse(input, out Isbn isbn))
    {
        Console.WriteLine($"valid: {isbn}");
        return 0;
    }

    Console.Error.WriteLine($"invalid ISBN-13: {input}");
    return 1;
}

static int FormatPrice(string amount)
{
    decimal value = decimal.Parse(amount, CultureInfo.InvariantCulture);
    Console.WriteLine(value.ToString("C", CultureInfo.GetCultureInfo("tr-TR")));
    return 0;
}

static int PrintUsage()
{
    Console.WriteLine("usage: bookstore-admin info | isbn <value> | price <amount>");
    return 2;
}
