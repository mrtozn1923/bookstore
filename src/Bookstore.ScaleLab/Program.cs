using System.Globalization;
using Bookstore.ScaleLab;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

return args switch
{
    ["load", .. var rest] => await LoadCommand.RunAsync(LabOptions.Parse(rest)),
    ["pool-leak", .. var rest] => await PoolCommands.LeakAsync(LabOptions.Parse(rest)),
    ["pool-frag", .. var rest] => await PoolCommands.FragmentAsync(LabOptions.Parse(rest)),
    _ => Usage()
};

static int Usage()
{
    Console.WriteLine("usage: load --db pg|sql --concurrency N --seconds S");
    return 1;
}
