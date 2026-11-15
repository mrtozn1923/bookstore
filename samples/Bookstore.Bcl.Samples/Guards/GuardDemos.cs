using System.Globalization;
using Bookstore.Bcl.Orders;
using Bookstore.Bcl.Text;

namespace Bookstore.Bcl.Samples.Guards;

public static class GuardDemos
{
    public static IEnumerable<(string Name, Action Run)> All =>
    [
        ("guard-guess", Guess),
        ("guard-compare", Compare),
        ("guard-messages", Messages),
        ("guard-custom", Custom),
        ("eq-hashcode", HashCodeDemo),
        ("eq-comparers", Comparers),
        ("lazy", LazyDemo),
    ];

    private static readonly Isbn Dune = Isbn.Parse("978-0-441-17271-9");
    private static readonly DateTimeOffset PlacedAt = new(2026, 11, 14, 9, 30, 0, TimeSpan.FromHours(3));

    private static void Print(Exception ex)
    {
        string paramName = ex is ArgumentException argument ? argument.ParamName ?? "(null)" : "-";
        Console.WriteLine($"  {ex.GetType().Name,-27} ParamName={paramName,-14} {ex.Message.ReplaceLineEndings(" | ")}");
    }

    private static void Guess()
    {
        try
        {
            _ = new OrderItem(Dune, 0, 349.90m);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine(ex.GetType().Name);
            Console.WriteLine(ex.ParamName);
            Console.WriteLine(ex.Message);
        }
    }

    private static void Compare()
    {
        OrderItem[] items = [new(Dune, 2, 349.90m)];
        (string Label, string OrderNumber, string Email, OrderItem[] Items, DateTimeOffset PlacedAt)[] cases =
        [
            ("blank order number", "  ", "ada@example.com", items, PlacedAt),
            ("bad e-mail", "BK-1001", "ada.example.com", items, PlacedAt),
            ("no items", "BK-1001", "ada@example.com", [], PlacedAt),
            ("default date", "BK-1001", "ada@example.com", items, default),
        ];

        foreach (var c in cases)
        {
            Console.WriteLine(c.Label);
            try { _ = new LegacyOrder(c.OrderNumber, c.Email, c.Items, c.PlacedAt); }
            catch (Exception ex) { Console.Write("  legacy:"); Print(ex); }
            try { _ = new Order(c.OrderNumber, c.Email, c.Items, c.PlacedAt); }
            catch (Exception ex) { Console.Write("  helper:"); Print(ex); }
        }
    }

    private static void Messages()
    {
        string? missing = null;
        IReadOnlyList<OrderItem> items = [];
        Run(() => ArgumentNullException.ThrowIfNull(missing));
        Run(() => ArgumentException.ThrowIfNullOrEmpty(""));
        Run(() => ArgumentOutOfRangeException.ThrowIfNegative(-5m));
        Run(() => ArgumentOutOfRangeException.ThrowIfGreaterThan(501, OrderItem.MaxQuantity));
        Run(() => ArgumentOutOfRangeException.ThrowIfZero(items.Count));
        Run(() => ArgumentOutOfRangeException.ThrowIfLessThan(PlacedAt, PlacedAt.AddDays(1)));

        OrderExport export = new();
        export.Dispose();
        Run(() => export.Write("BK-1001"));

        static void Run(Action action)
        {
            try { action(); }
            catch (Exception ex) { Print(ex); }
        }
    }

    private static void Custom()
    {
        var customer = new { Name = "Ada", Email = "ada(at)example.com" };
        try
        {
            Guard.Email(customer.Email);
        }
        catch (ArgumentException ex)
        {
            Print(ex);
        }

        try
        {
            _ = new OrderItem(default, 1, 99.90m);
        }
        catch (ArgumentException ex)
        {
            Print(ex);
        }
    }

    private static void HashCodeDemo()
    {
        OrderItem first = new(Dune, 2, 349.90m);
        OrderItem second = new(Isbn.Parse("9780441172719"), 2, 349.90m);
        Console.WriteLine($"record Equals: {first.Equals(second)}, ==: {first == second}, same hash: {first.GetHashCode() == second.GetHashCode()}");

        ShelfLocation location = new() { Aisle = "B", Shelf = 4 };
        HashSet<ShelfLocation> occupied = [location];
        Console.WriteLine($"Contains before change: {occupied.Contains(location)}");
        location.Aisle = "C";
        Console.WriteLine($"Contains after change : {occupied.Contains(location)}, Count: {occupied.Count}");
        Console.WriteLine($"Any(x => x.Equals(...)): {occupied.Any(x => x.Equals(location))}");
    }

    private static void Comparers()
    {
        string[] volumes = ["Cilt 10", "Cilt 2", "Cilt 1"];
        Console.WriteLine($"Ordinal         : {string.Join(", ", volumes.Order(StringComparer.Ordinal))}");
        StringComparer numeric = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering);
        Console.WriteLine($"NumericOrdering : {string.Join(", ", volumes.Order(numeric))}");

        Dictionary<string, decimal> coupons = new(StringComparer.OrdinalIgnoreCase) { ["KITAP10"] = 0.10m };
        Console.WriteLine($"OrdinalIgnoreCase finds 'kitap10': {coupons.ContainsKey("kitap10")}");
        StringComparer turkish = StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), ignoreCase: true);
        Console.WriteLine($"tr-TR ignoreCase 'kitap10' == 'KITAP10': {turkish.Equals("kitap10", "KITAP10")}");

        EqualityComparer<OrderItem> sameBook = EqualityComparer<OrderItem>.Create(
            (x, y) => x?.Isbn == y?.Isbn,
            item => item.Isbn.GetHashCode());
        OrderItem[] lines = [new(Dune, 1, 349.90m), new(Dune, 3, 329.90m), new(Isbn.Parse("9780140449136"), 1, 189.50m)];
        Console.WriteLine($"Distinct books (.NET 8 Create): {lines.Distinct(sameBook).Count()} of {lines.Length} lines");
    }

    private static void LazyDemo()
    {
        int loads = 0;
        Lazy<IReadOnlyDictionary<Isbn, decimal>> catalog = new(() =>
        {
            Interlocked.Increment(ref loads);
            return new Dictionary<Isbn, decimal> { [Dune] = 349.90m };
        });
        Parallel.For(0, 8, _ => _ = catalog.Value.Count);
        Console.WriteLine($"Lazy<T> factory runs: {loads}, IsValueCreated: {catalog.IsValueCreated}");

        int attempts = 0;
        Lazy<string> flaky = new(() =>
        {
            attempts++;
            if (attempts == 1)
                throw new TimeoutException("catalog service timed out");
            return "loaded";
        });
        for (int i = 0; i < 2; i++)
        {
            try { Console.WriteLine($"flaky.Value: {flaky.Value}"); }
            catch (TimeoutException ex) { Console.WriteLine($"attempt {i + 1}: {ex.Message} (factory runs: {attempts})"); }
        }

        string? settings = null;
        int initializations = 0;
        for (int i = 0; i < 3; i++)
            LazyInitializer.EnsureInitialized(ref settings, () => { initializations++; return "settings"; });
        Console.WriteLine($"LazyInitializer runs: {initializations}, value: {settings}");
    }

    private sealed class ShelfLocation : IEquatable<ShelfLocation>
    {
        public required string Aisle { get; set; }
        public required int Shelf { get; set; }

        public bool Equals(ShelfLocation? other) => other is not null && Aisle == other.Aisle && Shelf == other.Shelf;
        public override bool Equals(object? obj) => Equals(obj as ShelfLocation);
        public override int GetHashCode() => HashCode.Combine(Aisle, Shelf);
    }

    private sealed class OrderExport : IDisposable
    {
        private bool _disposed;

        public void Write(string orderNumber)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            Console.WriteLine(orderNumber);
        }

        public void Dispose() => _disposed = true;
    }
}
