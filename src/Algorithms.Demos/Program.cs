using System.Globalization;
using Algorithms.Demos.Demos;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

string demo = args.Length > 0 ? args[0] : "";
switch (demo)
{
    case "lru": CacheDemos.Basics(); break;
    case "lru-scan": CacheDemos.ScanWorkload(); break;
    case "lru-threads": CacheDemos.Threads(); break;
    case "memorycache": CacheDemos.MemoryCacheSizeLimit(); break;
    case "thumbnails": CacheDemos.Thumbnails(); break;
    default: Console.WriteLine("usage: dotnet run --project src/Algorithms.Demos -- <demo>"); break;
}
