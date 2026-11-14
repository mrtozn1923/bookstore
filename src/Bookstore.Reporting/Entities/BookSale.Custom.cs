namespace Bookstore.Reporting.Entities;

public partial class BookSale
{
    public bool IsBulkSale => Quantity >= 20;
}
