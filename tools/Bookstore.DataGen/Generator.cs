using System.Globalization;
using System.Text;

namespace Bookstore.DataGen;

public enum Segment { Loyal, Bargain, Occasional, Institutional }

public sealed record Customer(
    string Id, string FullName, string Email, int BirthYear, World.City City, string District,
    bool RemoteDistrict, DateTime SignupUtc, Segment Segment);

public sealed record OrderItem(string Isbn, int Quantity, decimal UnitPrice);

public sealed class Order
{
    public string Id { get; set; } = "";
    public required Customer Customer { get; init; }
    public DateTime OrderedUtc { get; init; }
    public string CityText { get; set; } = "";
    public string Warehouse { get; init; } = "";
    public int DistanceKm { get; init; }
    public string Carrier { get; init; } = "";
    public string ShippingMethod { get; init; } = "";
    public int ItemCount { get; init; }
    public double? WeightKg { get; set; }
    public decimal Total { get; set; }
    public decimal DiscountRate { get; init; }
    public bool IsCampaign { get; init; }
    public string PaymentMethod { get; init; } = "";
    public bool HasBackorder { get; init; }
    public string Note { get; init; } = "";
    public bool IsCancelled { get; init; }
    public double? DeliveryDays { get; set; }
    public DateTime? DeliveredUtc { get; set; }
    public int TrackingEvents { get; set; }
    public bool IsReturned { get; set; }
    public DateTime? ReturnRequestedUtc { get; set; }
    public bool Lost { get; set; }
    public List<OrderItem> Items { get; init; } = [];
}

public sealed record Review(string Id, string OrderId, string Isbn, int Rating, string Text, string Topic, DateTime CreatedUtc);

/// <summary>
/// Generates the synthetic Bookstore dataset. The rules below are the "ground truth" the models try to learn;
/// the posts never read them back into the models.
/// </summary>
public sealed class Generator(ulong seed, int customerCount)
{
    private readonly SplitMix64 _rng = new(seed);

    public List<Customer> Customers { get; } = [];
    public List<Order> Orders { get; } = [];
    public List<Review> Reviews { get; } = [];

    public void Run()
    {
        CreateCustomers();
        CreateOrders();
        AssignIdsAndInjectQualityIssues();
        CreateReviews();
    }

    private void CreateCustomers()
    {
        // Each district gets a fixed "remote" flag (about 12%): remote districts add transit time.
        Dictionary<string, bool> remote = [];
        foreach (World.City c in World.Cities)
        {
            for (int d = 1; d <= c.Districts; d++)
            {
                remote[$"{c.Code}-{d:00}"] = _rng.Chance(0.12);
            }
        }

        (World.City, double)[] cityWeights = [.. World.Cities.Select(c => (c, (double)c.Weight))];
        (Segment, double)[] segments =
            [(Segment.Loyal, 25), (Segment.Bargain, 28), (Segment.Occasional, 45), (Segment.Institutional, 2)];
        double span = (World.EndUtc.AddDays(-30) - World.StartUtc.AddDays(-365)).TotalDays;

        for (int i = 1; i <= customerCount; i++)
        {
            World.City city = _rng.PickWeighted(cityWeights);
            string district = $"{city.Code}-{_rng.Next(1, city.Districts + 1):00}";
            string first = _rng.Pick(World.FirstNames);
            string last = _rng.Pick(World.LastNames);
            Segment segment = _rng.PickWeighted(segments);
            DateTime signup = World.StartUtc.AddDays(-365 + _rng.NextDouble() * span).Date;
            Customers.Add(new Customer(
                $"C{i:00000}", $"{first} {last}", $"{first}.{last}.{i}@example.com".ToLowerInvariant(),
                _rng.Next(1950, 2008), city, district, remote[district], signup, segment));
        }
    }

    private static readonly double[] HourWeights =
        [1, 0.5, 0.3, 0.2, 0.2, 0.3, 0.6, 1.2, 2, 2.6, 3, 3.2, 3.3, 3.2, 3.1, 3, 3, 3.1, 3.3, 3.6, 4, 3.8, 3, 2];

    private static readonly double[] WeekdayFactor = [1.05, 1.1, 1.05, 1.0, 1.0, 0.95, 0.85]; // Sun..Sat

    private void CreateOrders()
    {
        Dictionary<string, int> orderCount = [];
        (int, double)[] hours = [.. HourWeights.Select((w, h) => (h, w))];
        double totalDays = (World.EndUtc - World.StartUtc).TotalDays;

        for (DateTime day = World.StartUtc.Date; day < World.EndUtc; day = day.AddDays(1))
        {
            bool campaign = World.IsCampaign(day);
            double mean = 24 + 12 * ((day - World.StartUtc).TotalDays / totalDays); // slow growth
            mean *= WeekdayFactor[(int)day.DayOfWeek];
            if (day.Month == 12) mean *= 1.25;
            if (campaign) mean *= 2.2;
            if (day == World.OutageDay) mean *= 0.06;
            if (day == World.PricingErrorDay) mean *= 3.2;
            int count = Math.Max(0, (int)Math.Round(mean + _rng.Normal() * Math.Sqrt(mean)));

            List<(Customer, double)> eligible = [.. Customers
                .Where(c => c.SignupUtc <= day)
                .Select(c => (c, ActivityWeight(c.Segment, day, campaign)))
                .Where(x => x.Item2 > 0)];
            if (eligible.Count == 0) continue;

            for (int i = 0; i < count; i++)
            {
                Customer customer = _rng.PickWeighted(eligible);
                int hour = _rng.PickWeighted(hours);
                DateTime local = day.AddHours(hour).AddMinutes(_rng.Next(0, 60)).AddSeconds(_rng.Next(0, 60));
                DateTime utc = DateTime.SpecifyKind(local - World.LocalOffset, DateTimeKind.Utc);
                int previous = orderCount.GetValueOrDefault(customer.Id);
                orderCount[customer.Id] = previous + 1;
                Orders.Add(CreateOrder(customer, utc, local, campaign, isFirstOrder: previous == 0));
            }
        }
        Orders.Sort((a, b) => a.OrderedUtc.CompareTo(b.OrderedUtc));
    }

    private static double ActivityWeight(Segment s, DateTime day, bool campaign) => s switch
    {
        Segment.Loyal => 6,
        Segment.Bargain => campaign ? 15 : 3,
        Segment.Occasional => day.Month == 12 ? 3 : 1,
        Segment.Institutional => day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? 0 : 3,
        _ => 1,
    };

    private Order CreateOrder(Customer c, DateTime utc, DateTime local, bool campaign, bool isFirstOrder)
    {
        bool institutional = c.Segment == Segment.Institutional;

        // Basket
        int itemCount = institutional ? _rng.Next(20, 121) : 1 + Geometric(c.Segment == Segment.Bargain ? 0.6 : 0.5, 11);
        List<OrderItem> items = [];
        int left = itemCount;
        int titles = institutional ? _rng.Next(1, 5) : itemCount;
        for (int t = 0; t < titles && left > 0; t++)
        {
            World.Book book = _rng.Pick(World.Books);
            int qty = t == titles - 1 ? left : institutional ? Math.Max(1, left / (titles - t)) : 1;
            left -= qty;
            items.Add(new OrderItem(book.Isbn, qty, book.Price));
        }
        double weight = items.Sum(i => i.Quantity * World.Books.First(b => b.Isbn == i.Isbn).WeightKg);
        decimal subtotal = items.Sum(i => i.Quantity * i.UnitPrice);

        decimal discount = 0m;
        if (institutional) discount = 0.10m;
        else if (campaign) discount = 0.15m + 0.05m * _rng.Next(0, 4);
        else if (c.Segment == Segment.Bargain && _rng.Chance(0.2)) discount = 0.10m;

        bool express = !institutional && _rng.Chance(c.Segment switch
        {
            Segment.Loyal => 0.20,
            Segment.Bargain => 0.05,
            _ => 0.25,
        });
        decimal fee = institutional ? 0m : (subtotal * (1 - discount) < 500m ? 49.90m : 0m) + (express ? 79.90m : 0m);
        decimal total = decimal.Round(subtotal * (1 - discount) + fee, 2);

        string payment = institutional ? "BankTransfer" : _rng.PickWeighted<string>(
        [
            ("CreditCard", c.Segment == Segment.Loyal ? 82 : 66),
            ("BankTransfer", 6),
            ("CashOnDelivery", c.Segment == Segment.Loyal ? 12 : 28),
        ]);
        string note = institutional ? "office address - weekdays only" : _rng.PickWeighted(World.Notes);
        if (!institutional && c.Segment == Segment.Occasional && local.Month == 12 && _rng.Chance(0.3))
        {
            note = "gift - do not include the invoice";
        }

        // Warehouse: nearest open one, sometimes the second nearest because of a stock shortage.
        string[] open = [.. World.Warehouses.Where(w => w != "IZM-1" || utc >= World.IzmirOpensUtc)];
        string[] byDistance = [.. open.OrderBy(w => World.DistanceKm(c.City, w))];
        string warehouse = _rng.Chance(0.12) ? byDistance[1] : byDistance[0];
        int distance = World.DistanceKm(c.City, warehouse);
        World.Carrier carrier = _rng.PickWeighted([.. World.Carriers.Select(x => (x, x.Share))]);

        double backorderChance = institutional ? 0.35 : 1 - Pow(0.985, itemCount);
        bool backorder = _rng.Chance(backorderChance);

        // Cancellation happens before shipping.
        double cancelChance = 0.02
            + (payment == "CashOnDelivery" ? 0.06 : 0)
            + (backorder ? 0.10 : 0)
            + (isFirstOrder ? 0.03 : 0)
            + (campaign ? 0.02 : 0)
            + (c.Segment == Segment.Bargain ? 0.02 : 0);
        bool cancelled = _rng.Chance(cancelChance);

        Order order = new()
        {
            Customer = c,
            OrderedUtc = utc,
            CityText = c.City.Name,
            Warehouse = warehouse,
            DistanceKm = distance,
            Carrier = carrier.Name,
            ShippingMethod = express ? "Express" : "Standard",
            ItemCount = itemCount,
            WeightKg = Math.Round(weight, 2),
            Total = total,
            DiscountRate = discount,
            IsCampaign = campaign,
            PaymentMethod = payment,
            HasBackorder = backorder,
            Note = note,
            IsCancelled = cancelled,
            Items = items,
        };
        if (cancelled)
        {
            return order;
        }

        // ---- Delivery time (hidden rules) ----
        // 1) Processing starts on the next working slot: cut-off 15:00 (Express 17:00), no weekend shifts.
        DateTime start = local;
        int cutoff = express ? 17 : 15;
        if (start.Hour >= cutoff) start = start.Date.AddDays(1).AddHours(9);
        while (start.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) start = start.Date.AddDays(1).AddHours(9);

        // 2) Handling time; campaign weeks overload the warehouse; back-ordered items wait for the supplier.
        double handlingHours = express ? _rng.Uniform(2, 4) : _rng.Uniform(4, 9);
        if (campaign) handlingHours += express ? _rng.Uniform(4, 10) : _rng.Uniform(12, 36);
        if (backorder) handlingHours += 24 * _rng.Uniform(3, 7);
        DateTime shipped = start.AddHours(handlingHours);

        // 3) Transit depends on carrier, distance, district and weight.
        double transit = carrier.BaseDays + distance / carrier.KmPerDay;
        if (carrier.Name == "CarrierB" && utc >= World.CarrierBSlowdownUtc) transit += 0.8;
        if (express) transit *= 0.55;
        if (c.RemoteDistrict) transit += 0.7;
        if (weight > 6) transit += 0.3;
        transit += _rng.Normal() * 0.18 * (1 + transit / 3);
        transit = Math.Max(0.15, transit);
        DateTime arrival = shipped.AddDays(transit);

        // 4) Carriers do not deliver on Sunday; office addresses accept parcels on weekdays only.
        if (arrival.DayOfWeek == DayOfWeek.Sunday) arrival = arrival.AddDays(1);
        if (note == "office address - weekdays only" && arrival.DayOfWeek == DayOfWeek.Saturday) arrival = arrival.AddDays(2);
        if (note == "deliver after 18:00" && arrival.Hour < 18) arrival = arrival.Date.AddHours(18 + _rng.Uniform(0, 2));

        // 5) A few parcels get lost and are found weeks later.
        bool lost = _rng.Chance(0.004);
        if (lost) arrival = arrival.AddDays(_rng.Uniform(15, 35));

        double days = Math.Round((arrival - local).TotalHours / 24, 2);
        int tracking = 3 + (int)(transit * 1.6) + (backorder ? 2 : 0) + _rng.Next(0, 2) + (lost ? 6 : 0);
        DateTime deliveredUtc = DateTime.SpecifyKind(arrival - World.LocalOffset, DateTimeKind.Utc);

        double returnChance = 0.025
            + (days > 6 ? 0.05 : 0)
            + (c.Segment == Segment.Bargain ? 0.03 : 0)
            + (institutional ? -0.02 : 0)
            + (lost ? 0.2 : 0);
        bool returned = _rng.Chance(returnChance);
        DateTime? returnRequested = returned
            ? DateTime.SpecifyKind(deliveredUtc.AddHours(24 * _rng.Uniform(1, 14)), DateTimeKind.Utc)
            : null;

        order.DeliveryDays = days;
        order.DeliveredUtc = deliveredUtc;
        order.TrackingEvents = tracking;
        order.IsReturned = returned;
        order.ReturnRequestedUtc = returnRequested;
        order.Lost = lost;
        return order;

    }

    private int Geometric(double stopProbability, int max)
    {
        int n = 0;
        while (n < max && !_rng.Chance(stopProbability)) n++;
        return n;
    }

    private static double Pow(double x, int n)
    {
        double r = 1;
        for (int i = 0; i < n; i++) r *= x;
        return r;
    }

    private void AssignIdsAndInjectQualityIssues()
    {
        for (int i = 0; i < Orders.Count; i++)
        {
            Orders[i].Id = $"O{i + 1:000000}";
        }

        // Real exports are never clean. These issues are what post 02 finds and fixes.
        foreach (Order o in Orders)
        {
            if (_rng.Chance(0.03)) o.WeightKg = null;                       // missing value
            if (_rng.Chance(0.012))                                          // inconsistent spelling
            {
                o.CityText = _rng.Next(0, 3) switch
                {
                    0 => o.CityText.ToLowerInvariant(),
                    1 => o.CityText.ToUpperInvariant(),
                    _ => o.CityText + " ",
                };
            }
        }
        List<Order> delivered = [.. Orders.Where(o => o.DeliveryDays is not null)];
        for (int i = 0; i < 6; i++)                                          // sign bug in the label
        {
            Order o = _rng.Pick(delivered);
            o.DeliveryDays = -o.DeliveryDays;
        }
        for (int i = 0; i < 4; i++)                                          // total stored in kuruş
        {
            Order o = _rng.Pick(Orders);
            o.Total *= 100;
        }

        // An export retry wrote some rows twice.
        List<Order> withDuplicates = new(Orders.Count + 200);
        foreach (Order o in Orders)
        {
            withDuplicates.Add(o);
            if (_rng.Chance(0.005)) withDuplicates.Add(o);
        }
        Orders.Clear();
        Orders.AddRange(withDuplicates);
    }

    private static readonly Dictionary<(string Topic, bool Positive), string[]> Sentences = new()
    {
        [("Shipping", false)] =
        [
            "The package arrived much later than promised.", "Delivery took forever and nobody called me.",
            "The courier left the box at the wrong address.", "Tracking said delivered but it came two days later.",
            "The box was crushed when it arrived.", "Shipping was really slow this time.",
            "I waited more than a week for the parcel.", "The cargo company lost my order for days.",
        ],
        [("Shipping", true)] =
        [
            "Arrived the next day and well packed.", "Fast delivery and the courier was polite.",
            "Came earlier than expected.", "Packaging was solid and delivery was quick.",
            "Delivered on time even during the campaign.",
        ],
        [("Price", false)] =
        [
            "Too expensive for a paperback.", "The price went up right after I ordered.",
            "The shipping fee is almost as much as the book.", "I found it cheaper elsewhere.",
            "Not worth the money at this price.",
        ],
        [("Price", true)] =
        [
            "Great price during the campaign.", "Good value for money.", "The discount made it a bargain.",
            "Cheaper than the shop near my office.", "Fair price for such a thick book.",
        ],
        [("Content", true)] =
        [
            "{0} is a masterpiece and I could not put it down.", "Beautiful story and the ending stayed with me.",
            "One of the best novels I have read.", "The characters feel so real.", "A classic for a reason.",
            "I read {0} in two days.",
        ],
        [("Content", false)] =
        [
            "The story was slow and boring.", "I did not like the ending of {0}.",
            "Hard to follow with too many characters.", "Not as good as everyone says.",
        ],
        [("PrintQuality", false)] =
        [
            "Pages are thin and the ink smudges.", "The cover was bent and some pages were loose.",
            "The font is tiny and hard to read.", "This edition has many typos.", "The binding fell apart after a week.",
        ],
        [("PrintQuality", true)] =
        [
            "Lovely hardcover with high quality paper.", "Print is crisp and the font is comfortable.",
            "Beautiful edition and nice to hold.",
        ],
    };

    private static readonly string[] Fillers =
        ["Ordered it for my son.", "Second copy I bought.", "Bought it as a gift.", "Recommended.", "Read it for my book club."];

    private static readonly string[] Topics = ["Shipping", "Price", "Content", "PrintQuality"];

    private void CreateReviews()
    {
        int n = 0;
        foreach (Order o in Orders.DistinctBy(o => o.Id))
        {
            if (o.DeliveryDays is not double days || !_rng.Chance(0.22)) continue;
            bool late = days > 5 || o.Lost;
            string topic = _rng.PickWeighted<string>(
            [
                ("Shipping", 0.25 + (late ? 0.35 : 0)),
                ("Content", 0.35),
                ("Price", 0.2 + (o.IsCampaign ? 0.1 : 0)),
                ("PrintQuality", 0.15),
            ]);
            bool positive = topic switch
            {
                "Shipping" => !late && _rng.Chance(0.85),
                "Price" => o.DiscountRate >= 0.15m || _rng.Chance(0.5),
                "Content" => _rng.Chance(0.7),
                _ => _rng.Chance(0.4),
            };
            OrderItem item = _rng.Pick(o.Items);
            string title = World.Books.First(b => b.Isbn == item.Isbn).Title;
            StringBuilder text = new(string.Format(CultureInfo.InvariantCulture, _rng.Pick(Sentences[(topic, positive)]), title));
            if (_rng.Chance(0.2))
            {
                string other = _rng.Pick(Topics.Where(t => t != topic).ToArray());
                text.Append(' ').Append(string.Format(CultureInfo.InvariantCulture,
                    _rng.Pick(Sentences[(other, _rng.Chance(0.5))]), title));
            }
            if (_rng.Chance(0.4)) text.Append(' ').Append(_rng.Pick(Fillers));
            int rating = _rng.Chance(0.1) ? 3 : positive ? _rng.Next(4, 6) : _rng.Next(1, 3);
            string label = _rng.Chance(0.04) ? _rng.Pick(Topics) : topic;           // label noise
            DateTime created = o.DeliveredUtc!.Value.AddHours(24 * _rng.Uniform(1, 20));
            Reviews.Add(new Review($"R{++n:00000}", o.Id, item.Isbn, rating, text.ToString(), label, created));
        }
    }
}
