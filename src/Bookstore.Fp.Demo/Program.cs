using System.Globalization;
using Bookstore.Fp.Demo;

// Same number format on every machine
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

string post = args.FirstOrDefault() ?? "";

switch (post)
{
    case "01-guess": Post01.Guess(); break;
    case "01": Post01.Declarative(); break;
    default: Console.WriteLine("usage: dotnet run --project src/Bookstore.Fp.Demo -- <01-guess|01>"); break;
}
