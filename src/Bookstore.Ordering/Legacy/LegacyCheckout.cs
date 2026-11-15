namespace Bookstore.Ordering.Legacy;

public class LegacyCheckout
{
    public static decimal LastTotal;

    public decimal CalculateTotal(List<LegacyCartLine> lines, string? couponCode, bool isMember)
    {
        // Clean up lines with zero quantity
        for (int i = lines.Count - 1; i >= 0; i--)
        {
            if (lines[i].Quantity <= 0)
                lines.RemoveAt(i);
        }

        decimal total = 0;
        bool hasPrintedBooks = false;
        foreach (LegacyCartLine line in lines)
        {
            total += line.UnitPrice * line.Quantity;
            if (!line.IsEbook)
                hasPrintedBooks = true;
        }

        if (couponCode == "AUTUMN10" && DateTime.Now < new DateTime(2026, 12, 1))
            total = total * 0.90m;

        if (isMember)
            total = total - 15;

        if (hasPrintedBooks && total < 300)
            total = total + 49.90m;

        total = Math.Round(total, 2, MidpointRounding.AwayFromZero);
        Console.WriteLine($"[checkout] total={total}");
        LastTotal = total;
        return total;
    }
}
