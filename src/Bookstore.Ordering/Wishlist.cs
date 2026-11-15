using System.Collections;

namespace Bookstore.Ordering;

// A customer's wishlist of ISBNs; the same book is kept only once.
public class Wishlist : IReadOnlyCollection<string>
{
    private readonly List<string> _isbns = [];

    public int Count => _isbns.Count;

    public void Add(string isbn)
    {
        if (!_isbns.Contains(isbn))
            _isbns.Add(isbn);
    }

    public void AddRange(IEnumerable<string> isbns)
    {
        foreach (string isbn in isbns)
            Add(isbn);
    }

    public bool Contains(string isbn) => _isbns.Contains(isbn);

    public IEnumerator<string> GetEnumerator() => _isbns.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
