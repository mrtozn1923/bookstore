using System.Globalization;
using Bookstore.RedisLab;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

string connectionString = Environment.GetEnvironmentVariable("BOOKSTORE_REDIS") ?? "localhost:6379";
string command = args.Length > 0 ? args[0] : "help";

switch (command)
{
    case "seed-keys":
        await EventLoopDemo.SeedKeysAsync(connectionString, int.Parse(args[1]));
        break;
    case "keys-vs-ping":
        await EventLoopDemo.KeysVersusPingAsync(connectionString);
        break;
    case "incr":
        await EventLoopDemo.ParallelIncrementAsync(connectionString);
        break;
    case "rtt":
        await EventLoopDemo.RoundTripAsync(connectionString);
        break;
    default:
        Console.WriteLine("commands: seed-keys <n> | keys-vs-ping | incr | rtt");
        break;
}
