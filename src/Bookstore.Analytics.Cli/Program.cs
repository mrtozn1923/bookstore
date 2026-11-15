using Bookstore.Analytics.Cli;

return args switch
{
    ["logs", string file, string host] => LogsCommand.Run(file, host),
    _ => Usage(),
};

static int Usage()
{
    Console.Error.WriteLine("usage: bookstore-analytics logs <access.log> <site-host>");
    return 2;
}
