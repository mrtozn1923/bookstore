namespace Bookstore.DataGen;

/// <summary>Static facts of the synthetic Bookstore world: cities, warehouses, carriers, books, names.</summary>
public static class World
{
    public static readonly DateTime StartUtc = new(2024, 11, 1, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime EndUtc = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Turkey has used UTC+3 all year since 2016.</summary>
    public static readonly TimeSpan LocalOffset = TimeSpan.FromHours(3);

    public static readonly string[] Warehouses = ["IST-1", "ANK-1", "IZM-1"];

    /// <summary>IZM-1 starts shipping on this date; before it only IST-1 and ANK-1 exist.</summary>
    public static readonly DateTime IzmirOpensUtc = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>CarrierB's new contract makes it slower from this date (a change the models must cope with).</summary>
    public static readonly DateTime CarrierBSlowdownUtc = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    public sealed record City(string Name, string Code, int Weight, int Districts, int KmIst, int KmAnk, int KmIzm);

    // Approximate road distances (km) from the three warehouses.
    public static readonly City[] Cities =
    [
        new("Istanbul", "IST", 30, 39, 25, 450, 480),
        new("Ankara", "ANK", 12, 25, 450, 20, 590),
        new("Izmir", "IZM", 9, 30, 480, 590, 20),
        new("Bursa", "BRS", 6, 17, 155, 385, 330),
        new("Kocaeli", "KOC", 5, 12, 110, 340, 540),
        new("Antalya", "ANT", 5, 19, 700, 480, 460),
        new("Adana", "ADA", 4, 15, 940, 490, 900),
        new("Konya", "KON", 4, 31, 660, 260, 560),
        new("Gaziantep", "GAZ", 3, 9, 1130, 680, 1110),
        new("Kayseri", "KAY", 3, 16, 770, 320, 870),
        new("Diyarbakir", "DIY", 3, 17, 1390, 930, 1360),
        new("Samsun", "SAM", 3, 17, 730, 410, 980),
        new("Eskisehir", "ESK", 3, 14, 330, 235, 410),
        new("Trabzon", "TRA", 2, 18, 1070, 740, 1300),
        new("Erzurum", "ERZ", 2, 20, 1240, 880, 1450),
        new("Van", "VAN", 2, 13, 1600, 1220, 1690),
    ];

    public static int DistanceKm(City city, string warehouse) => warehouse switch
    {
        "IST-1" => city.KmIst,
        "ANK-1" => city.KmAnk,
        _ => city.KmIzm,
    };

    /// <summary>Carrier speed model: fixed handover days + km per day of transit.</summary>
    public sealed record Carrier(string Name, double BaseDays, double KmPerDay, double Share);

    public static readonly Carrier[] Carriers =
    [
        new("CarrierA", 0.9, 900, 0.45),
        new("CarrierB", 0.6, 700, 0.35),
        new("CarrierC", 1.2, 1200, 0.20),
    ];

    public sealed record Book(string Isbn, string Title, decimal Price, double WeightKg);

    public static readonly Book[] Books =
    [
        new("9780441172719", "Dune", 389.90m, 0.62),
        new("9780141439587", "Emma", 189.90m, 0.48),
        new("9780547928227", "The Hobbit", 279.90m, 0.41),
        new("9780141439549", "Middlemarch", 249.90m, 0.78),
        new("9780141439518", "Pride and Prejudice", 159.90m, 0.38),
        new("9780142437247", "Moby-Dick", 229.90m, 0.71),
        new("9780141441146", "Jane Eyre", 179.90m, 0.52),
        new("9780140447934", "War and Peace", 449.90m, 1.10),
        new("9780060934347", "Don Quixote", 399.90m, 0.95),
        new("9780451524935", "Nineteen Eighty-Four", 199.90m, 0.33),
        new("9780743273565", "The Great Gatsby", 149.90m, 0.25),
        new("9780140449136", "Crime and Punishment", 269.90m, 0.60),
        new("9780141182803", "Mrs Dalloway", 169.90m, 0.30),
        new("9780140268867", "The Odyssey", 219.90m, 0.55),
        new("9780679783268", "Anna Karenina", 359.90m, 0.92),
        new("9780141439471", "Frankenstein", 139.90m, 0.29),
        new("9780486415871", "The Brothers Karamazov", 419.90m, 0.98),
        new("9780140439441", "Wuthering Heights", 149.90m, 0.36),
        new("9780156027328", "Life of Pi", 239.90m, 0.40),
        new("9780307387899", "The Road", 209.90m, 0.31),
    ];

    public static readonly string[] FirstNames =
        ["Ada", "Alan", "Grace", "Linus", "Barbara", "Edsger", "Margaret", "Dennis", "Frances", "Ken",
         "Hedy", "Donald", "Radia", "Niklaus", "Katherine", "Tim", "Annie", "John", "Mary", "Bjarne"];

    public static readonly string[] LastNames =
        ["Yilmaz", "Kaya", "Demir", "Sahin", "Celik", "Aydin", "Ozturk", "Arslan", "Dogan", "Kilic",
         "Aslan", "Cetin", "Kara", "Koc", "Kurt", "Ozdemir", "Polat", "Erdem", "Yildiz", "Tekin"];

    /// <summary>Free-text delivery notes; two of them change delivery time (hidden rule).</summary>
    public static readonly (string Note, double Weight)[] Notes =
    [
        ("", 72),
        ("please call before delivery", 6),
        ("leave with the doorman", 5),
        ("office address - weekdays only", 6),
        ("gift - do not include the invoice", 4),
        ("ring the bell twice", 3),
        ("deliver after 18:00", 4),
    ];

    /// <summary>Campaign weeks: more orders, more discount, busier warehouses.</summary>
    public static readonly (DateTime From, DateTime To)[] Campaigns =
    [
        (new(2024, 11, 25), new(2024, 12, 2)),
        (new(2025, 9, 1), new(2025, 9, 15)),
        (new(2025, 11, 24), new(2025, 12, 1)),
        (new(2026, 9, 1), new(2026, 9, 15)),
    ];

    public static bool IsCampaign(DateTime localDate) =>
        Campaigns.Any(c => localDate.Date >= c.From && localDate.Date < c.To);

    /// <summary>Two unusual days for the anomaly detection post: a site outage and a pricing mistake.</summary>
    public static readonly DateTime OutageDay = new(2025, 6, 14);
    public static readonly DateTime PricingErrorDay = new(2026, 2, 3);
}
