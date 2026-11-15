using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

// Usage: dotnet run --project samples/Bookstore.Bcl.Net11 -- <demo>
var demos = new Dictionary<string, Func<Task>>(StringComparer.OrdinalIgnoreCase)
{
    ["runes"] = Sync(RuneDemos.Run),
    ["hexfloat"] = Sync(HexFloatDemos.Run),
    ["numbers"] = Sync(NumberDemos.Run),
    ["base64"] = Sync(Base64Demos.Run),
    ["process"] = ProcessDemos.RunAsync,
};

if (args.Length == 0 || !demos.TryGetValue(args[0], out Func<Task>? run))
{
    Console.Error.WriteLine("Demos: " + string.Join(", ", demos.Keys.Order(StringComparer.Ordinal)));
    return 1;
}

await run();
return 0;

static Func<Task> Sync(Action action) => () =>
{
    action();
    return Task.CompletedTask;
};

static class RuneDemos
{
    public static void Run()
    {
        string shelfLabel = "📚 Dune 📚";
        Rune books = new(0x1F4DA);

        Console.WriteLine($"Length: {shelfLabel.Length} chars, {shelfLabel.EnumerateRunes().Count()} runes");
        Console.WriteLine($"Contains(Rune)  : {shelfLabel.Contains(books)}");
        Console.WriteLine($"IndexOf(Rune)   : {shelfLabel.IndexOf(books)}");
        Console.WriteLine($"LastIndexOf     : {shelfLabel.LastIndexOf(books)}");
        Console.WriteLine($"Trim(Rune)      : [{shelfLabel.Trim(books).Trim()}]");
        Console.WriteLine($"Split(Rune)     : {string.Join(" | ", "Dune📚Emma📚Middlemarch".Split(books, StringSplitOptions.None))}");
        Console.WriteLine($"Replace(Rune, Rune): {shelfLabel.Replace(books, new Rune('*'))}");
    }
}

static class HexFloatDemos
{
    public static void Run()
    {
        double price = 0.1 + 0.2;
        var invariant = System.Globalization.CultureInfo.InvariantCulture;
        var tr = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");

        string hex = price.ToString("X", invariant);
        double back = double.Parse(hex, System.Globalization.NumberStyles.HexFloat, invariant);

        Console.WriteLine($"G17 (invariant): {price.ToString("G17", invariant)}");
        Console.WriteLine($"X (invariant)  : {hex}");
        Console.WriteLine($"X (tr-TR)      : {price.ToString("X", tr)}");
        Console.WriteLine($"Round-trip equal: {back == price}");
    }
}

static class NumberDemos
{
    public static void Run()
    {
        var invariant = System.Globalization.CultureInfo.InvariantCulture;
        var seeded = new Random(2026);

        long orderSuffix = seeded.NextInteger<long>(1_000_000L, 10_000_000L);
        byte shelf = seeded.NextInteger<byte>((byte)10);
        UInt128 big = seeded.NextInteger<UInt128>();
        float weight = seeded.NextBinaryFloat<float>();
        Console.WriteLine($"NextInteger<long>   : {orderSuffix}");
        Console.WriteLine($"NextInteger<byte>   : {shelf}");
        Console.WriteLine($"NextInteger<UInt128>: {big}");
        Console.WriteLine($"NextBinaryFloat<float>: {weight.ToString(invariant)}");

        Console.WriteLine($"(decimal)0.1    = {((decimal)0.1).ToString(invariant)}");
        Console.WriteLine($"(decimal)349.90 = {((decimal)349.90).ToString(invariant)}");
        Console.WriteLine($"349.90m         = {349.90m.ToString(invariant)}");
    }
}

static class Base64Demos
{
    public static void Run()
    {
        byte[] token = [0xFB, 0xEF, 0xFF, 0x3E, 0x3F, 0x00, 0x10];

        string encoded = System.Buffers.Text.Base64.EncodeToString(token);
        byte[] decoded = System.Buffers.Text.Base64.DecodeFromChars(encoded);
        Console.WriteLine($"Base64.EncodeToString : {encoded}");
        Console.WriteLine($"Base64.DecodeFromChars: {Convert.ToHexString(decoded)}");
        Console.WriteLine($"Same as Convert       : {encoded == Convert.ToBase64String(token)}");
        Console.WriteLine($"Uri.UriSchemeData     : {Uri.UriSchemeData}");
    }
}

static class ProcessDemos
{
    public static async Task RunAsync()
    {
        string dir = Path.Combine(Path.GetTempPath(), "bookstore-nightly");
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, "orders.csv"), "OrderNumber,Total\n");

        ProcessStartInfo tar = new("tar", ["-czf", "nightly.tar.gz", "orders.csv", "missing.csv"])
        {
            WorkingDirectory = dir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        ProcessTextOutput result = await Process.RunAndCaptureTextAsync(tar);
        Console.WriteLine($"RunAndCaptureTextAsync: exit={result.ExitStatus.ExitCode} canceled={result.ExitStatus.Canceled}");
        Console.Write($"stderr: {result.StandardError}");

        ProcessExitStatus version = await Process.RunAsync("tar", ["--version"], silent: true);
        Console.WriteLine($"RunAsync(silent): exit={version.ExitCode}");

        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(1));
        ProcessExitStatus slow = await Process.RunAsync("sleep", ["30"], cancellationToken: cts.Token);
        Console.WriteLine($"sleep 30 with 1 s token: exit={slow.ExitCode} canceled={slow.Canceled} signal={slow.Signal}");

        using Process sleeper = Process.Start("sleep", "30");
        sleeper.Signal(PosixSignal.SIGTERM);
        ProcessExitStatus status = await sleeper.WaitForExitStatusAsync();
        Console.WriteLine($"SIGTERM: exit={status.ExitCode} signal={status.Signal}");

        if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
        {
            ProcessStartInfo child = new("sleep", ["1"]) { KillOnParentExit = true };
            using Process process = Process.Start(child)!;
            await process.WaitForExitAsync();
            Console.WriteLine($"KillOnParentExit: child exit={process.ExitCode}");
        }
    }
}
