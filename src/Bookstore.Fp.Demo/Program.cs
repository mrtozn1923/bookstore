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
    case "03-culture": Post03.Culture(); break;
    case "03-core": Post03.CoreAndShell(); break;
    case "03-memo": Post03.Memoization(); break;
    case "04-fold": Post04.Fold(); break;
    case "04-compose": Post04.Composition(); break;
    case "04-curry": Post04.Currying(); break;
    case "04-alloc": Post04.Allocations(); break;
    case "04-enum": Post04.MultipleEnumeration(); break;
    case "05-flags": Post05.Flags(); break;
    case "05-workflow": Post05.Workflow(); break;
    case "05-json": Post05.Json(); break;
    case "06-guess": Post06.Guess(); break;
    case "06-basics": Post06.Basics(); break;
    case "06-chain": Post06.Chain(); break;
    case "06-query": Post06.Query(); break;
    case "06-nested": Post06.Nested(); break;
    case "06-shapes": await Post06.Shapes(); break;
    case "06-pitfalls": Post06.Pitfalls(); break;
    default: Console.WriteLine("usage: dotnet run --project src/Bookstore.Fp.Demo -- <post-key>"); break;
}
