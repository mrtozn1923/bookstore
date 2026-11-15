namespace Bookstore.Web.Ordering;

public interface ICartStore
{
    IReadOnlyList<CartLine> Read(HttpContext context);
    void Write(HttpContext context, IReadOnlyList<CartLine> lines);
}
