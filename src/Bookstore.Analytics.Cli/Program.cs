using Bookstore.Analytics.Cli;

return args switch
{
    ["logs", string file, string host] => LogsCommand.Run(file, host),
    ["opportunities", string csv] => OpportunitiesCommand.Run(csv),
    _ => Usage(),
};

static int Usage()
{
    Console.Error.WriteLine("""
        usage:
          bookstore-analytics logs <access.log> <site-host>
          bookstore-analytics opportunities <pages-queries.csv>
        """);
    return 2;
}
