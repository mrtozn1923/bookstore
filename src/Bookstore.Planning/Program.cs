using System.Globalization;
using Bookstore.Planning;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

switch (args.FirstOrDefault())
{
    case "cone": Cone.Run(); break;
    case "count": PointsVsCount.Run(); break;
    default: Console.WriteLine("Usage: dotnet run --project src/Bookstore.Planning -- cone|count"); break;
}
