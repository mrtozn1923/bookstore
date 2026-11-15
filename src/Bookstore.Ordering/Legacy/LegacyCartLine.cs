namespace Bookstore.Ordering.Legacy;

public class LegacyCartLine
{
    public string Isbn { get; set; } = "";
    public string Title { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public bool IsEbook { get; set; }
}
