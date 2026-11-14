using System.Globalization;
using Bookstore.SearchLab;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

string command = args.Length > 0 ? args[0] : "help";
string[] rest = args.Skip(1).ToArray();

switch (command)
{
    case "inverted-index":
        InvertedIndexDemo.Run(rest);
        break;
    case "first-connection":
        await FirstConnectionDemo.RunAsync();
        break;
    case "create-index":
        await IndexSetup.RunAsync(rest);
        break;
    case "translators":
        await MappingDemo.TranslatorsAsync(rest);
        break;
    case "search":
        await SearchDemo.RunAsync(rest);
        break;
    case "bulk-compare":
        await BulkDemo.CompareAsync();
        break;
    case "bulk-all":
        BulkDemo.LoadAll(rest);
        break;
    case "bulk-errors":
        await BulkDemo.ErrorsAsync();
        break;
    case "refresh":
        await BulkDemo.RefreshAsync();
        break;
    case "concurrency":
        await BulkDemo.ConcurrencyAsync();
        break;
    case "export":
        await ExportDemo.RunAsync(rest);
        break;
    case "rank-eval":
        await RankEvalDemo.RunAsync(rest);
        break;
    case "syntax":
        await ClientDemo.SyntaxAsync();
        break;
    case "serialization":
        ClientDemo.Serialization();
        break;
    case "errors":
        await ClientDemo.ErrorsAsync();
        break;
    case "esql":
        await ClientDemo.EsqlAsync();
        break;
    case "templates":
        await IndexTemplates.RunAsync(rest);
        break;
    case "migrate":
        await IndexMigration.RunAsync(rest);
        break;
    case "ca-fingerprint":
        ProductionDemo.CaFingerprint(rest);
        break;
    case "tls-check":
        await ProductionDemo.TlsCheckAsync();
        break;
    case "seed-sql":
        await SqlSeeder.RunAsync(rest);
        break;
    default:
        Console.WriteLine("Commands: inverted-index [query], seed-sql [syntheticCount], first-connection, create-index [name], translators [name], search <text>, bulk-compare, bulk-all [count], bulk-errors, refresh, concurrency, export [pageSize], rank-eval [options], syntax, serialization, errors, esql, templates [--replicas n], migrate <new-index> [--rps n], ca-fingerprint <file>, tls-check");
        break;
}
