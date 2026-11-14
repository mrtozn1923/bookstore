using System.Globalization;
using Bookstore.IO.Xml;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

string command = args.Length > 0 ? args[0] : "help";

switch (command)
{
    case "write": XmlDemos.Write(); break;
    case "read": XmlDemos.Read(); break;
    case "validate": XmlDemos.Validate(); break;
    case "batch": await XmlDemos.BatchAsync(args.Length > 1 ? args[1] : "stream", args.Length > 2 ? int.Parse(args[2]) : 200_000); break;
    case "serializers": XmlDemos.Serializers(); break;
    case "serializer-cache": XmlDemos.SerializerCache(); break;
    case "secure": XmlDemos.Secure(); break;
    default:
        Console.WriteLine("Commands: write | read | validate | batch <write|stream|xdocument> [count] | serializers | serializer-cache | secure");
        break;
}
