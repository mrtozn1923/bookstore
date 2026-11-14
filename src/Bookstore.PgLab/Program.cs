using Npgsql;
using Bookstore.PgLab;

string connectionString = Environment.GetEnvironmentVariable("BOOKSTORE_PG")
    ?? throw new InvalidOperationException("Set the BOOKSTORE_PG environment variable.");

switch (args.FirstOrDefault())
{
    case "connections":
        await ConnectionDemo.OpenUntilRejectedAsync(connectionString);
        break;
    case "pool":
        await ConnectionDemo.RunThroughPoolAsync(connectionString, maxPoolSize: 20, requests: 200);
        break;
    case "ef-script":
        EfDefaults.PrintCreateScript(connectionString);
        break;
    default:
        Console.WriteLine("usage: dotnet run -- connections | pool | ef-script");
        break;
}
