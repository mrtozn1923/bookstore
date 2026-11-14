namespace Bookstore.Data.Entities;

// Row type returned by the dbo.fn_BestSellers table-valued function.
public class BestSeller
{
    public int BookId { get; set; }
    public int Units { get; set; }
}
