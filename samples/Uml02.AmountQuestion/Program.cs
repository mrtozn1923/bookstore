int quantity = 120;          // The Hobbit, dört sınıf için
decimal listPrice = 450m;    // TL

decimal listTotal = quantity * listPrice;
decimal payable = ApplyDiscount(listTotal, discountPercent: 10);

Console.WriteLine($"List total: {listTotal}, needs purchasing manager: {listTotal > 50_000m}");
Console.WriteLine($"Payable:    {payable}, needs purchasing manager: {payable > 50_000m}");

static decimal ApplyDiscount(decimal amount, int discountPercent) =>
    amount - amount * discountPercent / 100;
