using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Bookstore.DataGen;

// Usage: dotnet run --project tools/Bookstore.DataGen -- [outputDir] [seed]
string outDir = args.Length > 0 ? args[0] : "data";
ulong seed = args.Length > 1 ? ulong.Parse(args[1], CultureInfo.InvariantCulture) : 2026UL;
Directory.CreateDirectory(outDir);

Generator gen = new(seed, customerCount: 4000);
gen.Run();

CultureInfo inv = CultureInfo.InvariantCulture;
static string Iso(DateTime d) => d.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
static string Bool(bool b) => b ? "true" : "false";
static string Quote(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";

Write("customers.csv", "CustomerId,FullName,Email,BirthYear,City,District,SignupDateUtc",
    gen.Customers.Select(c => string.Join(',', c.Id, c.FullName, c.Email, c.BirthYear.ToString(inv), c.City.Name,
        c.District, c.SignupUtc.ToString("yyyy-MM-dd", inv))));

Write("orders.csv",
    "OrderId,CustomerId,OrderedAtUtc,City,District,Warehouse,DistanceKm,Carrier,ShippingMethod,ItemCount,TotalWeightKg," +
    "OrderTotal,DiscountRate,IsCampaign,PaymentMethod,HasBackorderItem,CustomerNote,IsCancelled,DeliveryDays," +
    "TrackingEventCount,IsReturned,ReturnRequestedUtc",
    gen.Orders.Select(o => string.Join(',',
        o.Id, o.Customer.Id, Iso(o.OrderedUtc), o.CityText, o.Customer.District, o.Warehouse,
        o.DistanceKm.ToString(inv), o.Carrier, o.ShippingMethod, o.ItemCount.ToString(inv),
        o.WeightKg?.ToString("0.00", inv) ?? "", o.Total.ToString("0.00", inv), o.DiscountRate.ToString("0.00", inv),
        Bool(o.IsCampaign), o.PaymentMethod, Bool(o.HasBackorder), o.Note, Bool(o.IsCancelled),
        o.DeliveryDays?.ToString("0.00", inv) ?? "", o.IsCancelled ? "" : o.TrackingEvents.ToString(inv),
        Bool(o.IsReturned), o.ReturnRequestedUtc is DateTime r ? Iso(r) : "")));

Write("order_items.csv", "OrderId,Isbn,Quantity,UnitPrice",
    gen.Orders.DistinctBy(o => o.Id).SelectMany(o => o.Items.Select(i =>
        string.Join(',', o.Id, i.Isbn, i.Quantity.ToString(inv), i.UnitPrice.ToString("0.00", inv)))));

Write("reviews.csv", "ReviewId,OrderId,Isbn,Rating,Text,Topic,CreatedAtUtc",
    gen.Reviews.Select(r => string.Join(',', r.Id, r.OrderId, r.Isbn, r.Rating.ToString(inv), Quote(r.Text), r.Topic,
        Iso(r.CreatedUtc))));

Console.WriteLine($"seed {seed}: {gen.Customers.Count} customers, {gen.Orders.Count} order rows, {gen.Reviews.Count} reviews");
foreach (string file in new[] { "customers.csv", "orders.csv", "order_items.csv", "reviews.csv" })
{
    byte[] hash = SHA256.HashData(File.ReadAllBytes(Path.Combine(outDir, file)));
    Console.WriteLine($"{file,-16} {Convert.ToHexStringLower(hash)[..16]}");
}

void Write(string name, string header, IEnumerable<string> rows)
{
    // "\n" line endings and invariant culture: the same bytes on Windows, Linux and macOS.
    StringBuilder sb = new(header);
    sb.Append('\n');
    foreach (string row in rows)
    {
        sb.Append(row).Append('\n');
    }
    File.WriteAllText(Path.Combine(outDir, name), sb.ToString(), new UTF8Encoding(false));
}
