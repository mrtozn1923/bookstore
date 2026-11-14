using System.Globalization;
using Bookstore.ScaleLab;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

return args switch
{
    ["load", .. var rest] => await LoadCommand.RunAsync(LabOptions.Parse(rest)),
    _ => Usage()
};

static int Usage()
{
    Console.WriteLine("usage: load --db pg|sql --concurrency N --seconds S");
    return 1;
}
