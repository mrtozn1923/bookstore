// Bookstore'un rapor aracını taklit eden küçük bir CLI:
// veri stdout'a, ilerleme mesajları stderr'e yazılır
string command = args.Length > 0 ? args[0] : "";

switch (command)
{
    case "sales":
        int rows = int.Parse(args[1]);
        Console.Out.WriteLine("isbn,title,qty,total");
        for (int i = 1; i <= rows; i++)
        {
            Console.Out.WriteLine($"9780441172719,Dune,{i % 5 + 1},{(i % 5 + 1) * 420.00m:F2}");
            Console.Error.WriteLine($"progress: row {i} of {rows} written");
        }
        if (args.Contains("--fail"))
        {
            Console.Error.WriteLine("error: warehouse data is incomplete");
            return 2;
        }
        return 0;

    case "sleep":
        Thread.Sleep(TimeSpan.FromSeconds(int.Parse(args[1])));
        return 0;

    case "args":
        foreach (string arg in args.Skip(1))
        {
            Console.WriteLine($"[{arg}]");
        }
        return 0;

    case "env":
        string? value = Environment.GetEnvironmentVariable(args[1]);
        Console.WriteLine(value is null ? $"{args[1]} is not set" : $"{args[1]}={value}");
        return 0;

    default:
        Console.Error.WriteLine("usage: sales N [--fail] | sleep S | args ... | env NAME");
        return 1;
}
