using Bookstore.Ordering;

namespace Bookstore.Ordering.Tests;

public class CustomerReferenceTests
{
    private static void AwardPoints(IEnumerable<Order> orders)
    {
        foreach (Order order in orders)
            order.Customer.EarnPoints(10);
    }

    [Fact]
    public void Copies_of_the_same_customer_lose_points()
    {
        Order first = new() { Customer = new Customer { Id = 7, Name = "Ada" } };
        Order second = new() { Customer = new Customer { Id = 7, Name = "Ada" } };

        AwardPoints([first, second]);

        Assert.Equal(10, first.Customer.LoyaltyPoints);
        Assert.Equal(10, second.Customer.LoyaltyPoints);
    }

    [Fact]
    public void Orders_from_the_repository_share_one_customer()
    {
        CustomerRepository customers = new();
        Order first = new() { Customer = customers.Get(7, "Ada") };
        Order second = new() { Customer = customers.Get(7, "Ada") };

        AwardPoints([first, second]);

        Assert.Same(first.Customer, second.Customer);
        Assert.Equal(20, customers.Get(7, "Ada").LoyaltyPoints);
    }
}
