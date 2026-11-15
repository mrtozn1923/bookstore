using System.Globalization;
using Bookstore.Fp.Demo;

// Same number format on every machine
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

string post = args.FirstOrDefault() ?? "";

switch (post)
{
    case "01-guess": Post01.Guess(); break;
    case "01": Post01.Declarative(); break;
    case "02-aliasing": Post02.Aliasing(); break;
    case "02-cache": Post02.CacheKey(); break;
    case "02-records": Post02.Records(); break;
    case "02-shallow": Post02.Shallow(); break;
    case "02-cost": Post02.CopyCost(); break;
    default: Console.WriteLine("usage: dotnet run --project src/Bookstore.Fp.Demo -- <post-key>"); break;
}
