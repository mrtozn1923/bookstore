using System.Runtime.InteropServices;

namespace Bookstore.Bcl.Samples.Runtime;

public static class EnvironmentDemos
{
    public static IEnumerable<(string Name, Action Run)> All =>
    [
        ("env-guess", Guess),
        ("env-info", Info),
        ("env-vars", Variables),
        ("env-folders", Folders),
        ("env-appcontext", AppContextDemo),
    ];

    private static void Guess()
    {
        // BOOKSTORE_REPORT_DIR is set in the environment: /tmp/reports
        Console.WriteLine(Environment.NewLine.Length);
        Console.WriteLine(Path.Combine("reports", "2026-11-14.csv"));
        Console.WriteLine(Environment.GetEnvironmentVariable("bookstore_report_dir") ?? "(null)");
    }

    private static void Info()
    {
        Console.WriteLine($"OperatingSystem.IsWindows() : {OperatingSystem.IsWindows()}");
        Console.WriteLine($"OperatingSystem.IsLinux()   : {OperatingSystem.IsLinux()}");
        Console.WriteLine($"OSDescription               : {RuntimeInformation.OSDescription}");
        Console.WriteLine($"FrameworkDescription        : {RuntimeInformation.FrameworkDescription}");
        Console.WriteLine($"RuntimeIdentifier           : {RuntimeInformation.RuntimeIdentifier}");
        Console.WriteLine($"OS / Process architecture   : {RuntimeInformation.OSArchitecture} / {RuntimeInformation.ProcessArchitecture}");
        Console.WriteLine($"Is64BitProcess              : {Environment.Is64BitProcess}");
        Console.WriteLine($"ProcessorCount              : {Environment.ProcessorCount}");
        Console.WriteLine($"MachineName / UserName      : {Environment.MachineName} / {Environment.UserName}");
        Console.WriteLine($"ProcessId                   : {Environment.ProcessId}");
        Console.WriteLine($"Version                     : {Environment.Version}");
    }

    private static void Variables()
    {
        string reportDir = Environment.GetEnvironmentVariable("BOOKSTORE_REPORT_DIR") ?? Path.Combine(Path.GetTempPath(), "bookstore-reports");
        Console.WriteLine($"BOOKSTORE_REPORT_DIR      : {reportDir}");

        string? rawRetention = Environment.GetEnvironmentVariable("BOOKSTORE_REPORT_KEEP_DAYS");
        int keepDays = int.TryParse(rawRetention, out int parsed) && parsed > 0 ? parsed : 30;
        Console.WriteLine($"BOOKSTORE_REPORT_KEEP_DAYS: raw={rawRetention ?? "(null)"} -> {keepDays}");

        Environment.SetEnvironmentVariable("BOOKSTORE_RUN_ID", "nightly-2026-11-14");
        Console.WriteLine($"After Set (process)       : {Environment.GetEnvironmentVariable("BOOKSTORE_RUN_ID")}");

        int bookstoreVariables = Environment.GetEnvironmentVariables().Keys.Cast<string>().Count(k => k.StartsWith("BOOKSTORE_", StringComparison.Ordinal));
        Console.WriteLine($"BOOKSTORE_* variables     : {bookstoreVariables}");

        try
        {
            Environment.SetEnvironmentVariable("BOOKSTORE_RUN_ID", "x", EnvironmentVariableTarget.User);
            Console.WriteLine($"Target=User on Unix       : no exception, read back: {Environment.GetEnvironmentVariable("BOOKSTORE_RUN_ID", EnvironmentVariableTarget.User) ?? "(null)"}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Target=User               : {ex.GetType().Name}");
        }
    }

    private static void Folders()
    {
        Console.WriteLine($"CurrentDirectory      : {Environment.CurrentDirectory}");
        Console.WriteLine($"AppContext.BaseDirectory: {AppContext.BaseDirectory}");
        Console.WriteLine($"GetTempPath()         : {Path.GetTempPath()}");
        Environment.SpecialFolder[] folders =
        [
            Environment.SpecialFolder.UserProfile,
            Environment.SpecialFolder.ApplicationData,
            Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolder.CommonApplicationData,
            Environment.SpecialFolder.ProgramFiles,
        ];
        foreach (Environment.SpecialFolder folder in folders)
        {
            string verified = Environment.GetFolderPath(folder);
            string doNotVerify = Environment.GetFolderPath(folder, Environment.SpecialFolderOption.DoNotVerify);
            Console.WriteLine($"{folder,-22}: \"{verified}\" / DoNotVerify: \"{doNotVerify}\"");
        }
    }

    private static void AppContextDemo()
    {
        bool found = AppContext.TryGetSwitch("Bookstore.Reports.UseLegacyCsv", out bool useLegacyCsv);
        Console.WriteLine($"Bookstore.Reports.UseLegacyCsv : found={found}, value={useLegacyCsv}");

        found = AppContext.TryGetSwitch("System.Globalization.Invariant", out bool invariant);
        Console.WriteLine($"System.Globalization.Invariant : found={found}, value={invariant}");
        Console.WriteLine($"System.GC.Server (GetData)     : {AppContext.GetData("System.GC.Server") ?? "(null)"}");

        AppContext.SetSwitch("Bookstore.Reports.IncludeCancelled", true);
        AppContext.TryGetSwitch("Bookstore.Reports.IncludeCancelled", out bool includeCancelled);
        Console.WriteLine($"Set in code                    : {includeCancelled}");
    }
}
