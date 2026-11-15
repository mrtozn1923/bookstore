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
    case "kmp": StringDemos.WorstCase(); break;
    case "moderation": StringDemos.Moderation(); break;
    case "authors": StringDemos.Authors(); break;
    case "turkish": StringDemos.Turkish(); break;
    case "plagiarism": StringDemos.Plagiarism(); break;
    case "campaigns": IntervalDemos.Campaigns(); break;
    case "slots": IntervalDemos.DeliverySlots(); break;
    case "shifts": IntervalDemos.Shifts(); break;
    case "lowest-price": IntervalDemos.LowestPrice(); break;
    case "timezones": IntervalDemos.TimeZones(); break;
    case "bloom": ProbabilisticDemos.Bloom(); break;
    case "top-viewed": ProbabilisticDemos.TopViewed(); break;
    case "unique-visitors": ProbabilisticDemos.UniqueVisitors(); break;
    default: Console.WriteLine("usage: dotnet run --project src/Algorithms.Demos -- <demo>"); break;
}
