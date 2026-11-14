using System.Diagnostics;
using System.Text;

namespace Bookstore.OsLab;

public sealed record FlushResult(string Mode, int Orders, double Milliseconds);

public static class FsLab
{
    public static byte[] OrderLine(int i) =>
        Encoding.UTF8.GetBytes($"{DateTime.UtcNow:O};order-{i};9780441172719;1;420.00;TRY\n");

    // Her siparişten sonra bir flush: toDisk=true ise fsync de yapılır
    public static FlushResult WriteOrders(string path, int count, bool toDisk)
    {
        using FileStream fs = new(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < count; i++)
        {
            fs.Write(OrderLine(i));
            fs.Flush(flushToDisk: toDisk);
        }
        return new FlushResult(toDisk ? "Flush(true)" : "Flush()", count, sw.Elapsed.TotalMilliseconds);
    }

    // Açık file descriptor'ları sayar: /proc/self/fd'deki her girdi bir fd'dir
    public static int OpenDescriptors() =>
        OperatingSystem.IsLinux() ? Directory.GetFiles("/proc/self/fd").Length : -1;

    public static (int Opened, string Error) OpenUntilFailure(string directory)
    {
        List<FileStream> open = [];
        try
        {
            for (int i = 0; ; i++)
            {
                open.Add(new FileStream(Path.Combine(directory, $"invoice-{i}.pdf"), FileMode.Create));
            }
        }
        catch (IOException ex)
        {
            return (open.Count, ex.Message);
        }
        finally
        {
            foreach (FileStream fs in open)
            {
                fs.Dispose();
            }
        }
    }

    public static IEnumerable<string> DeleteWhileOpen(string path)
    {
        File.WriteAllText(path, "Dune;Emma;The Hobbit\n");
        using FileStream reader = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

        File.Delete(path);
        yield return $"File.Exists after delete: {File.Exists(path)}";

        using StreamReader text = new(reader);
        yield return $"still readable: {text.ReadLine()}";

        if (OperatingSystem.IsLinux())
        {
            foreach (string fd in Directory.GetFiles("/proc/self/fd"))
            {
                string? target = new FileInfo(fd).LinkTarget;
                if (target is not null && target.Contains(Path.GetFileName(path)))
                {
                    yield return $"fd {Path.GetFileName(fd)} -> {target}";
                }
            }
        }
    }

    // Önce geçici dosyaya yaz, diske indir, sonra tek bir rename ile yerine koy
    public static void AtomicSave(string path, string content)
    {
        string temp = path + ".tmp";
        using (FileStream fs = new(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            fs.Write(Encoding.UTF8.GetBytes(content));
            fs.Flush(flushToDisk: true);
        }
        File.Move(temp, path, overwrite: true);
    }
}
