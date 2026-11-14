using Bogus;
using Bookstore.Ordering.Institutional;

namespace Bookstore.Catalog.Tests;

public sealed class CustomerFakerTests
{
    [Fact]
    public void Positional_records_are_created_through_the_custom_instantiator()
    {
        Faker<CustomerAccount> customers = new Faker<CustomerAccount>("tr")
            .UseSeed(2026)
            .CustomInstantiator(f => new CustomerAccount(
                f.Internet.ExampleEmail(),
                IsInstitutional: f.Random.Bool(0.2f),
                CompletedOrders: f.Random.Int(0, 40)));

        List<CustomerAccount> accounts = customers.Generate(200);

        Assert.All(accounts, a => Assert.Matches(@"@example\.(com|org|net)$", a.Email));
        Assert.Contains(accounts, a => a.IsInstitutional);
    }
}
