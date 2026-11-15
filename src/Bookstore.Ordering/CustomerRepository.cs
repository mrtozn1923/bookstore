namespace Bookstore.Ordering;

public sealed class CustomerRepository
{
    private readonly Dictionary<int, Customer> _byId = [];

    public Customer Get(int id, string name)
    {
        if (!_byId.TryGetValue(id, out Customer? customer))
        {
            customer = new Customer { Id = id, Name = name };
            _byId.Add(id, customer);
        }
        return customer;
    }
}
