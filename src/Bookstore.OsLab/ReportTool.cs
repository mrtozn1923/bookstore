using System.Diagnostics;

namespace Bookstore.OsLab;

public static class ReportTool
{
    public static string DllPath => Path.Combine(AppContext.BaseDirectory, "Bookstore.ReportTool.dll");

    public static string[] Args(params string[] args) => [DllPath, .. args];

    // YANLIŞ: önce stdout'u sonuna kadar okur, stderr'e kimse bakmaz
    public static string RunNaive(int rows)
    {
        ProcessStartInfo psi = new("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (string argument in Args("sales", rows.ToString()))
        {
            psi.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(psi)!;
        string csv = process.StandardOutput.ReadToEnd();
        string log = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return csv;
    }
}
