using System.Diagnostics;
using Bookstore.OsLab;

string command = args.Length > 0 ? args[0] : "help";

switch (command)
{
    case "read-catalog":
        string json = File.ReadAllText("data/catalog.json");
        Console.WriteLine($"catalog.json: {json.Length} chars");
        break;

    case "log-naive":
        OrderLog.WriteNaive("orders.log", int.Parse(args[1]));
        break;

    case "log-buffered":
        OrderLog.WriteBuffered("orders.log", int.Parse(args[1]));
        break;

    case "proc-info":
        Console.WriteLine($"pid={Environment.ProcessId}, sleeping {args[1]} s");
        Thread.Sleep(TimeSpan.FromSeconds(int.Parse(args[1])));
        break;

    case "report-naive":
        string csv = ReportTool.RunNaive(int.Parse(args[1]));
        Console.WriteLine($"rows={csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length - 1}");
        break;

    case "report":
    {
        string[] toolArgs = ReportTool.Args(["sales", .. args[1..]]);
        Stopwatch sw = Stopwatch.StartNew();
        ProcessResult r = await ProcessRunner.RunAsync("dotnet", toolArgs, TimeSpan.FromSeconds(10));
        string lastLog = r.StdErr.TrimEnd().Split('\n')[^1];
        Console.WriteLine($"exit={r.ExitCode} timedOut={r.TimedOut} stdout={r.StdOut.Length} B stderr={r.StdErr.Length} B in {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"last stderr line: {lastLog}");
        break;
    }

    case "report-timeout":
    {
        Stopwatch sw = Stopwatch.StartNew();
        ProcessResult r = await ProcessRunner.RunAsync("dotnet", ReportTool.Args("sleep", "30"), TimeSpan.FromSeconds(2));
        Console.WriteLine($"exit={r.ExitCode} timedOut={r.TimedOut} after {sw.ElapsedMilliseconds} ms");
        break;
    }

    case "shell-demo":
    {
        string author = args[1];

        // YANLIŞ: kullanıcı girdisi bir shell komutunun içine gömülüyor
        ProcessResult viaShell = await ProcessRunner.RunAsync("/bin/sh",
            ["-c", $"dotnet {ReportTool.DllPath} args {author}"], TimeSpan.FromSeconds(10));
        Console.Write($"via /bin/sh -c:{Environment.NewLine}{viaShell.StdOut}");

        // DOĞRU: shell yok; author tek bir argüman olarak gider
        ProcessResult direct = await ProcessRunner.RunAsync("dotnet",
            ReportTool.Args("args", author), TimeSpan.FromSeconds(10));
        Console.Write($"via ArgumentList:{Environment.NewLine}{direct.StdOut}");
        break;
    }

    case "env-demo":
    {
        string[] toolArgs = ReportTool.Args("env", "ConnectionStrings__BookstoreDb");
        ProcessResult inherited = await ProcessRunner.RunAsync("dotnet", toolArgs, TimeSpan.FromSeconds(10));
        ProcessResult hidden = await ProcessRunner.RunAsync("dotnet", toolArgs, TimeSpan.FromSeconds(10),
            hiddenVariables: ["ConnectionStrings__BookstoreDb"]);
        Console.Write($"default:  {inherited.StdOut}hidden:   {hidden.StdOut}");
        break;
    }

    default:
        Console.WriteLine("usage: read-catalog | log-naive N | log-buffered N | proc-info S | report-naive N | report N [--fail] | report-timeout | shell-demo TEXT | env-demo");
        break;
}
