using System.Diagnostics;
using System.Runtime.InteropServices;
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

    case "threads":
    {
        ThreadLab.Describe("main");
        Thread exporter = new(() =>
        {
            ThreadLab.Describe("OrderExporter");
            Thread.Sleep(TimeSpan.FromSeconds(int.Parse(args[1])));
        }) { Name = "OrderExporter" };
        exporter.Start();
        await Task.Run(() => ThreadLab.Describe("thread pool"));
        Console.WriteLine($"pid={Environment.ProcessId}, sleeping {args[1]} s");
        exporter.Join();
        break;
    }

    case "thread-cost":
    {
        int count = int.Parse(args[1]);
        int stack = args.Length > 2 ? int.Parse(args[2]) * 1024 : 0;
        ThreadCost c = ThreadLab.Measure(count, stack);
        Console.WriteLine($"{c.Waiters} threads (maxStackSize={(stack == 0 ? "default" : $"{stack / 1024} KB")}): " +
            $"{c.Milliseconds:F1} ms, os threads={c.OsThreads}, virtual +{c.VirtualDeltaMb} MB, resident +{c.ResidentDeltaMb} MB");
        break;
    }

    case "task-cost":
    {
        ThreadCost c = await ThreadLab.MeasureTasksAsync(int.Parse(args[1]));
        Console.WriteLine($"{c.Waiters} awaiting tasks: {c.Milliseconds:F1} ms, os threads={c.OsThreads}, " +
            $"virtual +{c.VirtualDeltaMb} MB, resident +{c.ResidentDeltaMb} MB");
        break;
    }

    case "context":
        Console.WriteLine(await ContextDemo.RunAsync());
        break;

    case "cpu-info":
        Console.WriteLine($"ProcessorCount={Environment.ProcessorCount}");
        if (File.Exists("/sys/fs/cgroup/cpu.max"))
        {
            Console.WriteLine($"cpu.max={File.ReadAllText("/sys/fs/cgroup/cpu.max").Trim()}");
        }
        break;

    case "workers":
    {
        int count = int.Parse(args[1]);
        TimeSpan duration = TimeSpan.FromSeconds(int.Parse(args[2]));
        foreach (WorkerReport r in SchedLab.RunWorkers(count, duration))
        {
            Console.WriteLine($"{r.Name}: {r.Iterations / 1_000_000,6:N0} M iterations, " +
                $"voluntary={r.Voluntary}, nonvoluntary={r.Involuntary}");
        }
        break;
    }

    case "spin":
    {
        if (args.Length > 2)
        {
            using Process self = Process.GetCurrentProcess();
            self.PriorityClass = Enum.Parse<ProcessPriorityClass>(args[2]);
        }
        long iterations = SchedLab.Spin(TimeSpan.FromSeconds(int.Parse(args[1])));
        Console.WriteLine($"pid={Environment.ProcessId} {(args.Length > 2 ? args[2] : "Normal"),-11} {iterations / 1_000_000,6:N0} M iterations");
        break;
    }

    case "pingpong":
    {
        int rounds = int.Parse(args[1]);
        (TimeSpan elapsed, long voluntary) = SchedLab.PingPong(rounds);
        Console.WriteLine($"{rounds:N0} round trips in {elapsed.TotalMilliseconds:F0} ms " +
            $"({elapsed.TotalMicroseconds / rounds:F2} us each), voluntary switches={voluntary:N0}");
        break;
    }

    case "nice":
    {
        await Task.Run(() => { }); // thread pool thread'leri oluşsun
        using (Process self = Process.GetCurrentProcess())
        {
            self.PriorityClass = ProcessPriorityClass.BelowNormal;
        }
        Thread late = new(() => Thread.Sleep(500)) { Name = "created-after" };
        late.Start();
        foreach ((string tid, string name, int nice) in SchedLab.ThreadNiceValues())
        {
            Console.WriteLine($"{tid,6} {name,-16} nice={nice}");
        }
        late.Join();
        break;
    }

    case "paging":
        foreach (string line in MemLab.DemandPaging(int.Parse(args[1])))
        {
            Console.WriteLine(line);
        }
        break;

    case "mem-info":
    {
        using Process self = Process.GetCurrentProcess();
        Console.WriteLine($"SystemPageSize      = {Environment.SystemPageSize}");
        Console.WriteLine($"VirtualMemorySize64 = {self.VirtualMemorySize64 >> 20} MB");
        Console.WriteLine($"WorkingSet64        = {self.WorkingSet64 >> 20} MB");
        Console.WriteLine($"PrivateMemorySize64 = {self.PrivateMemorySize64 >> 20} MB");
        Console.WriteLine($"GC.GetTotalMemory   = {GC.GetTotalMemory(false) >> 10} KB");
        break;
    }

    case "overcommit":
    {
        long size = long.Parse(args[1]) << 30;
        try
        {
            IntPtr block = Marshal.AllocHGlobal((nint)size);
            Console.WriteLine($"{args[1]} GB reserved, nothing touched: {MemLab.Snapshot()}");
            Marshal.FreeHGlobal(block);
        }
        catch (OutOfMemoryException)
        {
            Console.WriteLine($"{args[1]} GB: OutOfMemoryException (malloc returned NULL)");
        }
        break;
    }

    case "oom":
    {
        long size = long.Parse(args[1]) << 20;
        IntPtr block = Marshal.AllocHGlobal((nint)size);
        Console.WriteLine($"allocated {args[1]} MB: {MemLab.Snapshot()}");
        long step = 64L << 20;
        for (long done = 0; done < size; done += step)
        {
            MemLab.TouchPages(block + (nint)done, Math.Min(step, size - done));
            Console.WriteLine($"touched {(done + step) >> 20,5} MB: {MemLab.Snapshot()}");
        }
        Marshal.FreeHGlobal(block);
        break;
    }

    case "gc-config":
        foreach (KeyValuePair<string, object> kv in GC.GetConfigurationVariables())
        {
            if (kv.Key is "GCRegionRange" or "GCHeapHardLimit")
            {
                Console.WriteLine($"{kv.Key} = {kv.Value}");
            }
        }
        break;

    case "gc-limits":
    {
        string[] keys = ["GCHeapHardLimit", "GCRegionRange", "HeapCount", "GCDynamicAdaptationMode"];
        IReadOnlyDictionary<string, object> vars = GC.GetConfigurationVariables();
        foreach (string key in keys)
        {
            Console.WriteLine($"{key,-23} = {(vars.TryGetValue(key, out object? v) ? v : "-")}");
        }
        Console.WriteLine($"{"IsServerGC",-23} = {System.Runtime.GCSettings.IsServerGC}");
        Console.WriteLine($"{"TotalAvailableMemory",-23} = {GC.GetGCMemoryInfo().TotalAvailableMemoryBytes >> 20} MB");
        Console.WriteLine($"{"memory.max",-23} = {MemLab.CgroupValue("memory.max")}");
        break;
    }

    case "fill":
        foreach (string line in MemLab.FillManaged(int.Parse(args[1])))
        {
            Console.WriteLine(line);
        }
        break;

    case "churn":
        Console.WriteLine(MemLab.Churn(int.Parse(args[1])));
        break;

    case "map-share":
        foreach (string line in MapLab.Share(args[1], int.Parse(args[2]), TimeSpan.FromSeconds(int.Parse(args[3]))))
        {
            Console.WriteLine(line);
        }
        break;

    case "cow":
        foreach (string line in MapLab.CopyOnWrite(args[1], int.Parse(args[2])))
        {
            Console.WriteLine(line);
        }
        break;

    case "layout":
        foreach (string line in MapLab.Layout())
        {
            Console.WriteLine(line);
        }
        break;

    case "big-maps":
        foreach (string line in MapLab.LargeMappings(long.Parse(args[1])))
        {
            Console.WriteLine(line);
        }
        break;

    case "wx":
    {
        (int writable, int executable, int both) = MapLab.Permissions();
        Console.WriteLine($"mappings: writable={writable} executable={executable} writable+executable={both}");
        break;
    }

    default:
        Console.WriteLine("usage: read-catalog | log-naive N | log-buffered N | proc-info S | report-naive N | report N [--fail] | report-timeout | shell-demo TEXT | env-demo | threads S | thread-cost N [stackKB] | task-cost N | context | cpu-info | workers N S | spin S [PriorityClass] | pingpong N | nice | paging MB | mem-info | overcommit GB | oom MB | gc-config | gc-limits | fill MB | churn MB | map-share FILE MB S | cow FILE MB | layout | big-maps MB | wx");
        break;
}
