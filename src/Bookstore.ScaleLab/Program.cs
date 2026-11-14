using System.Globalization;
using Bookstore.ScaleLab;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

return args switch
{
    ["load", .. var rest] => await LoadCommand.RunAsync(LabOptions.Parse(rest)),
    ["pool-leak", .. var rest] => await PoolCommands.LeakAsync(LabOptions.Parse(rest)),
    ["pool-frag", .. var rest] => await PoolCommands.FragmentAsync(LabOptions.Parse(rest)),
    ["archive", .. var rest] => await ArchiveCommand.RunAsync(LabOptions.Parse(rest)),
    ["failover", .. var rest] => await FailoverCommand.RunAsync(LabOptions.Parse(rest)),
    ["reconnect", .. var rest] => await ResiliencyCommands.ReconnectAsync(LabOptions.Parse(rest)),
    ["user-tx", .. var rest] => await ResiliencyCommands.UserTransactionAsync(LabOptions.Parse(rest)),
    _ => Usage()
};

static int Usage()
{
    Console.WriteLine("usage: load --db pg|sql --concurrency N --seconds S");
    return 1;
}
