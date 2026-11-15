using System.Runtime.InteropServices;
using Bookstore.Contracts;

return args switch
{
    ["info"] => PrintInfo(),
    ["isbn", string input] => CheckIsbn(input),
    _ => PrintUsage(),
};

static int PrintInfo()
{
    Console.WriteLine($"Runtime:     {RuntimeInformation.FrameworkDescription}");
    Console.WriteLine($"Environment: {Environment.GetEnvironmentVariable("BOOKSTORE_ENVIRONMENT") ?? "(not set)"}");
    return 0;
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

static int PrintUsage()
{
    Console.WriteLine("usage: bookstore-admin info | isbn <value>");
    return 2;
}
