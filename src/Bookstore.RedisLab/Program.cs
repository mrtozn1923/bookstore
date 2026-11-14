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
    case "mux-per-request":
        await ClientDemo.MultiplexerPerRequestAsync(connectionString, dispose: args.Length < 2 || args[1] != "--leak");
        break;
    case "mux-singleton":
        await ClientDemo.SingletonAsync(connectionString);
        break;
    case "pipeline":
        await ClientDemo.PipelineAsync(connectionString);
        break;
    case "pubsub-connection":
        await ClientDemo.SubscriptionConnectionAsync(connectionString);
        break;
    case "scan-vs-ping":
        await ClientDemo.ScanVersusPingAsync(connectionString);
        break;
    case "server-info":
        await ClientDemo.ServerInfoAsync(connectionString);
        break;
    case "key-names":
        await ClientDemo.KeyNameCostAsync(connectionString);
        break;
    case "lost-update":
        await StringDemo.LostUpdateAsync(connectionString);
        break;
    case "active-expire":
        await StringDemo.ActiveExpireAsync(connectionString);
        break;
    case "ttl-reset":
        await StringDemo.TtlResetAsync(connectionString);
        break;
    case "cart-storage":
        await HashDemo.CartStorageAsync(connectionString);
        break;
    case "hash-encoding":
        await HashDemo.EncodingAsync(connectionString);
        break;
    case "big-hash":
        await HashDemo.BigHashAsync(connectionString);
        break;
    case "compact-hash":
        await HashDemo.CompactHashAsync(connectionString);
        break;
    case "queue-crash":
        await ListSetDemo.QueueCrashAsync(connectionString);
        break;
    case "blpop":
        await ListSetDemo.BlockingPopAsync(connectionString);
        break;
    case "visitors":
        await ListSetDemo.VisitorsAsync(connectionString);
        break;
    case "big-set":
        await ListSetDemo.BigSetAsync(connectionString);
        break;
    case "bestsellers":
        await SortedSetDemo.BestsellersAsync(connectionString);
        break;
    case "seed-week":
        await SortedSetDemo.SeedWeekAsync(connectionString);
        break;
    case "zset-encoding":
        await SortedSetDemo.EncodingAsync(connectionString);
        break;
    case "score-precision":
        await SortedSetDemo.ScorePrecisionAsync(connectionString);
        break;
    case "page-drift":
        await SortedSetDemo.PageDriftAsync(connectionString);
        break;
    case "recent-orders":
        await SortedSetDemo.RecentOrdersAsync(connectionString);
        break;
    case "sync-over-async":
        await SyncOverAsyncDemo.RunAsync(connectionString);
        break;
    default:
        Console.WriteLine("commands: seed-keys <n> | keys-vs-ping | incr | rtt | mux-per-request [--leak] | mux-singleton | " +
                          "pipeline | pubsub-connection | scan-vs-ping | server-info | key-names | sync-over-async | lost-update | active-expire | ttl-reset | cart-storage | hash-encoding | big-hash | compact-hash | queue-crash | blpop | visitors | big-set | bestsellers | seed-week | zset-encoding | score-precision | page-drift | recent-orders");
        break;
}
