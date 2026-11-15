using System.Reflection;
using DbUp;
using DbUp.Engine;

string connectionString = Environment.GetEnvironmentVariable("BOOKSTORE_PRICES_DB")
    ?? throw new InvalidOperationException("Set the BOOKSTORE_PRICES_DB environment variable.");

EnsureDatabase.For.SqlDatabase(connectionString);

UpgradeEngine upgrader = DeployChanges.To
    .SqlDatabase(connectionString)
    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
    .WithTransactionPerScript()
    .LogToConsole()
    .Build();

DatabaseUpgradeResult result = upgrader.PerformUpgrade();
if (!result.Successful)
{
    Console.Error.WriteLine(result.Error);
    return 1;
}

Console.WriteLine($"Applied scripts: {result.Scripts.Count()}");
return 0;
