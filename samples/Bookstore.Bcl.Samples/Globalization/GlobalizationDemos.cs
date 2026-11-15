using System.Globalization;
using Bookstore.Bcl.Text;

namespace Bookstore.Bcl.Samples.Globalization;

public static class GlobalizationDemos
{
    public static IEnumerable<(string Name, Func<Task> Run)> All =>
    [
        ("culture-guess", Sync(Guess)),
        ("culture-hierarchy", Sync(Hierarchy)),
        ("culture-current", Sync(CurrentVsUi)),
        ("culture-async", FlowAsync),
        ("culture-sort", Sync(Sort)),
        ("culture-case", Sync(Casing)),
        ("culture-search", Sync(Search)),
        ("culture-dictionary", Sync(DictionaryKeys)),
        ("culture-mode", Sync(Mode)),
    ];

    private static Func<Task> Sync(Action action) => () =>
    {
        action();
        return Task.CompletedTask;
    };

    private static readonly string[] Titles =
    [
        "Zübük", "Çalıkuşu", "Cevdet Bey ve Oğulları", "İnce Memed", "Ivanhoe", "Şeker Portakalı",
        "Saatleri Ayarlama Enstitüsü", "Ulysses", "Üç İstanbul", "emma", "Dune",
    ];

    private static void Guess()
    {
        string[] titles = ["Zübük", "Çalıkuşu", "Cevdet Bey ve Oğulları", "İnce Memed", "Ivanhoe"];
        Array.Sort(titles);
        Console.WriteLine(string.Join(", ", titles));
        Console.WriteLine("istanbul".ToUpper());
        Console.WriteLine("istanbul".Equals("İSTANBUL", StringComparison.OrdinalIgnoreCase));
    }

    private static void Hierarchy()
    {
        CultureInfo culture = CultureInfo.GetCultureInfo("tr-TR");
        while (true)
        {
            string name = culture.Name.Length == 0 ? "\"\" (invariant)" : culture.Name;
            Console.WriteLine($"{name,-16} neutral={culture.IsNeutralCulture,-5} EnglishName={culture.EnglishName}, NativeName={culture.NativeName}");
            if (culture.Equals(CultureInfo.InvariantCulture))
                break;
            culture = culture.Parent;
        }

        CultureInfo cached = CultureInfo.GetCultureInfo("tr-TR");
        CultureInfo created = new("tr-TR");
        Console.WriteLine($"GetCultureInfo: ReadOnly={cached.IsReadOnly}, same instance={ReferenceEquals(cached, CultureInfo.GetCultureInfo("tr-TR"))}");
        Console.WriteLine($"new CultureInfo: ReadOnly={created.IsReadOnly}, UseUserOverride={created.UseUserOverride}");

        // ICU accepts any well-formed name and falls back to root data instead of throwing.
        foreach (string name in new[] { "tr-XX", "xx-YY" })
        {
            try
            {
                CultureInfo odd = CultureInfo.GetCultureInfo(name);
                Console.WriteLine($"{name}: EnglishName={odd.EnglishName}, D={new DateTime(2026, 11, 14).ToString("D", odd)}");
            }
            catch (CultureNotFoundException ex)
            {
                Console.WriteLine($"{name}: {ex.GetType().Name}: {ex.Message.Split('\n')[0]} InvalidCultureName={ex.InvalidCultureName}");
            }
        }
    }

    private static void CurrentVsUi()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

        decimal total = 1249.50m;
        DateTime placed = new(2026, 11, 14);
        Console.WriteLine($"CurrentCulture={CultureInfo.CurrentCulture.Name}, CurrentUICulture={CultureInfo.CurrentUICulture.Name}");
        Console.WriteLine($"Formatted with CurrentCulture: {total:N2} {placed:D}");
        Console.WriteLine($"UI text picked by CurrentUICulture: {OrderPlacedMessage()}");

        static string OrderPlacedMessage() => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
        {
            "tr" => "Siparişiniz alındı",
            _ => "Your order has been placed",
        };
    }

    private static async Task FlowAsync()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("en-GB");
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
        Print("Main, before await");

        await Task.Delay(10);
        Print("Main, after await");

        await Task.Run(() => Print("Task.Run"));

        await ChangeInsideAsync();
        Print("Main, after ChangeInsideAsync");

        var thread = new Thread(() => Print("new Thread"));
        thread.Start();
        thread.Join();

        var done = new TaskCompletionSource();
        ThreadPool.UnsafeQueueUserWorkItem(_ =>
        {
            Print("UnsafeQueueUserWorkItem");
            done.SetResult();
        }, null);
        await done.Task;

        static async Task ChangeInsideAsync()
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            await Task.Yield();
            Print("Inside ChangeInsideAsync");
        }

        static void Print(string where) =>
            Console.WriteLine($"{where,-30} thread {Environment.CurrentManagedThreadId,2}  {CultureInfo.CurrentCulture.Name}  {1234.5m:N2}");
    }

    private static void Sort()
    {
        List<string> ordinal = [.. Titles];
        ordinal.Sort(StringComparer.Ordinal);
        List<string> invariant = [.. Titles];
        invariant.Sort(StringComparer.InvariantCulture);
        List<string> turkish = [.. Titles];
        turkish.Sort(StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), ignoreCase: false));

        Console.WriteLine($"{"Ordinal",-30}{"InvariantCulture",-30}tr-TR");
        for (int i = 0; i < Titles.Length; i++)
            Console.WriteLine($"{ordinal[i],-30}{invariant[i],-30}{turkish[i]}");
    }

    private static void Casing()
    {
        CultureInfo tr = CultureInfo.GetCultureInfo("tr-TR");
        CultureInfo en = CultureInfo.GetCultureInfo("en-US");

        Console.WriteLine($"\"istanbul\".ToUpper(tr)        = {"istanbul".ToUpper(tr)}");
        Console.WriteLine($"\"istanbul\".ToUpper(en)        = {"istanbul".ToUpper(en)}");
        Console.WriteLine($"\"istanbul\".ToUpperInvariant() = {"istanbul".ToUpperInvariant()}");
        Console.WriteLine($"\"ISBN\".ToLower(tr)            = {"ISBN".ToLower(tr)}");
        Console.WriteLine($"\"ISBN\".ToLowerInvariant()     = {"ISBN".ToLowerInvariant()}");

        string query = "istanbul";
        string stored = "İSTANBUL";
        Console.WriteLine($"OrdinalIgnoreCase             : {string.Equals(query, stored, StringComparison.OrdinalIgnoreCase)}");
        Console.WriteLine($"InvariantCultureIgnoreCase    : {string.Equals(query, stored, StringComparison.InvariantCultureIgnoreCase)}");
        Console.WriteLine($"tr-TR IgnoreCase              : {string.Compare(query, stored, tr, CompareOptions.IgnoreCase) == 0}");
        Console.WriteLine($"en-US IgnoreCase              : {string.Compare(query, stored, en, CompareOptions.IgnoreCase) == 0}");
        Console.WriteLine($"tr-TR IgnoreCase \"ISTANBUL\"   : {string.Compare(query, "ISTANBUL", tr, CompareOptions.IgnoreCase) == 0}");
    }

    private static void Search()
    {
        string[] catalog = ["İSTANBUL HATIRALAR VE ŞEHİR", "Kürk Mantolu Madonna", "İnce Memed", "Istanbul: Memories and the City"];
        string[] queries = ["istanbul", "sehir", "kurk", "ince"];
        CompareInfo turkish = CultureInfo.GetCultureInfo("tr-TR").CompareInfo;
        CompareInfo invariant = CultureInfo.InvariantCulture.CompareInfo;
        const CompareOptions options = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

        foreach (string query in queries)
        {
            Console.WriteLine($"{query,-9} tr-TR     -> {Hits(title => turkish.IndexOf(title, query, options) >= 0)}");
            Console.WriteLine($"{"",-9} Invariant -> {Hits(title => invariant.IndexOf(title, query, options) >= 0)}");
            string foldedQuery = SearchText.Fold(query);
            Console.WriteLine($"{"",-9} Fold      -> {Hits(title => SearchText.Fold(title).Contains(foldedQuery, StringComparison.Ordinal))}");
        }

        string Hits(Func<string, bool> match) => string.Join(" | ", catalog.Where(match));
    }

    private static void DictionaryKeys()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
        var templates = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase)
        {
            ["INVOICE"] = "invoice.html",
            ["SHIPPING"] = "shipping.html",
        };

        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        Console.WriteLine($"comparer created under tr-TR, lookup \"invoice\": {templates.ContainsKey("invoice")}");
        Console.WriteLine($"comparer created under tr-TR, lookup \"shipping\": {templates.ContainsKey("shipping")}");

        var safe = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["INVOICE"] = "invoice.html",
        };
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
        Console.WriteLine($"OrdinalIgnoreCase under tr-TR, lookup \"invoice\": {safe.ContainsKey("invoice")}");
    }

    private static void Mode()
    {
        Console.WriteLine($"ICU loaded       : {IsIcuLoaded()}");
        Console.WriteLine($"CurrentCulture   : \"{CultureInfo.CurrentCulture.Name}\"");

        try
        {
            CultureInfo tr = CultureInfo.GetCultureInfo("tr-TR");
            DateTime placed = new(2026, 11, 14);
            Console.WriteLine($"tr-TR date       : {placed.ToString("D", tr)}");
            Console.WriteLine($"tr-TR money      : {1234.5m.ToString("C2", tr)}");
            Console.WriteLine($"ToUpper(tr)      : {"istanbul".ToUpper(tr)}");
            Console.WriteLine($"Sort(tr)         : {string.Join(", ", Titles.Take(5).Order(StringComparer.Create(tr, false)))}");
        }
        catch (CultureNotFoundException ex)
        {
            Console.WriteLine($"{ex.GetType().Name}: {ex.Message.Split('\n')[0]}");
        }

        Console.WriteLine($"IndexOf(\"\\0\")    : {"Dune\0\0".IndexOf("\0", StringComparison.CurrentCulture)}");
        Console.WriteLine($"ToUpperInvariant : {"çalıkuşu".ToUpperInvariant()}");
    }

    // From the .NET docs: ICU is in use when the sort version reports an ICU id.
    private static bool IsIcuLoaded()
    {
        SortVersion sortVersion = CultureInfo.InvariantCulture.CompareInfo.Version;
        byte[] bytes = sortVersion.SortId.ToByteArray();
        int version = bytes[3] << 24 | bytes[2] << 16 | bytes[1] << 8 | bytes[0];
        return version != 0 && version == sortVersion.FullVersion;
    }
}
