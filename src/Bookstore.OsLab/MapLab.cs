using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace Bookstore.OsLab;

public static class MapLab
{
    // /proc/self/smaps_rollup: process'in bütün mapping'lerinin toplamı (kB)
    public static Dictionary<string, long> Rollup()
    {
        Dictionary<string, long> values = [];
        foreach (string line in File.ReadLines("/proc/self/smaps_rollup"))
        {
            string[] parts = line.Split(':', 2);
            if (parts.Length == 2 && parts[1].TrimEnd().EndsWith(" kB"))
            {
                values[parts[0]] = long.Parse(parts[1].Trim()[..^3].Trim());
            }
        }
        return values;
    }

    public static string RollupLine()
    {
        Dictionary<string, long> r = Rollup();
        return $"Rss={r["Rss"] / 1024,4} MB  Pss={r["Pss"] / 1024,4} MB  " +
               $"Shared_Clean={r["Shared_Clean"] / 1024,4} MB  Private_Dirty={r["Private_Dirty"] / 1024,4} MB";
    }

    public static void CreateCatalogExport(string path, int megabytes)
    {
        if (File.Exists(path) && new FileInfo(path).Length == (long)megabytes << 20)
        {
            return;
        }
        byte[] line = "9780441172719;Dune;Frank Herbert;420.00;TRY\n"u8.ToArray();
        using FileStream fs = new(path, FileMode.Create);
        long size = (long)megabytes << 20;
        for (long written = 0; written < size; written += line.Length)
        {
            fs.Write(line, 0, (int)Math.Min(line.Length, size - written));
        }
    }

    // Dosyayı salt okunur map eder ve her sayfasını bir kez okur
    public static long ReadEveryPage(MemoryMappedViewAccessor view, long size)
    {
        long sum = 0;
        int page = Environment.SystemPageSize;
        for (long offset = 0; offset < size; offset += page)
        {
            sum += view.ReadByte(offset);
        }
        return sum;
    }

    public static IEnumerable<string> Share(string path, int megabytes, TimeSpan hold)
    {
        CreateCatalogExport(path, megabytes);
        long size = (long)megabytes << 20;
        yield return $"before map     {RollupLine()}";

        using MemoryMappedFile file = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
        using MemoryMappedViewAccessor view = file.CreateViewAccessor(0, size, MemoryMappedFileAccess.Read);
        ReadEveryPage(view, size);
        yield return $"pages read     {RollupLine()}";

        Thread.Sleep(hold);
        yield return $"after {hold.TotalSeconds,2:F0} s     {RollupLine()}";
    }

    public static IEnumerable<string> CopyOnWrite(string path, int megabytes)
    {
        CreateCatalogExport(path, megabytes);
        long size = (long)megabytes << 20;
        using MemoryMappedFile file = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 0, MemoryMappedFileAccess.CopyOnWrite);
        using MemoryMappedViewAccessor view = file.CreateViewAccessor(0, size, MemoryMappedFileAccess.CopyOnWrite);

        ReadEveryPage(view, size);
        yield return $"read only      {RollupLine()}";

        // Her sayfanın ilk byte'ını değiştir: kernel sayfanın özel bir kopyasını çıkarır
        int page = Environment.SystemPageSize;
        for (long offset = 0; offset < size; offset += page)
        {
            view.Write(offset, (byte)'#');
        }
        yield return $"after writes   {RollupLine()}";
        yield return $"in view: '{(char)view.ReadByte(0)}'  on disk: '{(char)File.ReadAllBytes(path)[0]}'";
    }

    // /proc/self/maps içinde verilen ada sahip ilk satırın başlangıç adresi
    public static string FirstAddress(string name)
    {
        foreach (string line in File.ReadLines("/proc/self/maps"))
        {
            if (line.EndsWith(name))
            {
                return line[..line.IndexOf('-')];
            }
        }
        return "-";
    }

    public static IEnumerable<string> Layout()
    {
        IntPtr small = Marshal.AllocHGlobal(64);
        IntPtr large = Marshal.AllocHGlobal(16 << 20);
        yield return $"dotnet host     {FirstAddress("/dotnet")}";
        yield return $"[heap]          {FirstAddress("[heap]")}";
        yield return $"malloc(64)      {small:x}";
        yield return $"malloc(16 MB)   {large:x}";
        yield return $"libcoreclr.so   {FirstAddress("libcoreclr.so")}";
        yield return $"[stack]         {FirstAddress("[stack]")}";
        Marshal.FreeHGlobal(small);
        Marshal.FreeHGlobal(large);
    }

    public static (int Writable, int Executable, int Both) Permissions()
    {
        int writable = 0, executable = 0, both = 0;
        foreach (string line in File.ReadLines("/proc/self/maps"))
        {
            string perms = line.Split(' ')[1];
            bool w = perms[1] == 'w', x = perms[2] == 'x';
            if (w) writable++;
            if (x) executable++;
            if (w && x) both++;
        }
        return (writable, executable, both);
    }
    // Büyük adres aralıklarını boyut, izin ve dosya adıyla listeler
    public static IEnumerable<string> LargeMappings(long minMegabytes)
    {
        foreach (string line in File.ReadLines("/proc/self/maps"))
        {
            string[] parts = line.Split(' ', 6, StringSplitOptions.RemoveEmptyEntries);
            string[] range = parts[0].Split('-');
            long size = Convert.ToInt64(range[1], 16) - Convert.ToInt64(range[0], 16);
            if (size >= minMegabytes << 20)
            {
                string name = parts.Length > 5 ? parts[5] : "(anonymous)";
                yield return $"{size >> 20,8} MB  {parts[1]}  {name}";
            }
        }
    }
}
