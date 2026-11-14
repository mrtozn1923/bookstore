using Bookstore.ResilienceLab.Demos;

// Usage: dotnet run --project samples/Bookstore.ResilienceLab -- <demo>
var demos = new Dictionary<string, Func<Task>>
{
    ["clients"] = ClientsDemo.RunAsync
};

if (args.Length == 0 || !demos.TryGetValue(args[0], out var demo))
{
    Console.WriteLine($"Demos: {string.Join(", ", demos.Keys)}");
    return;
}

await demo();
