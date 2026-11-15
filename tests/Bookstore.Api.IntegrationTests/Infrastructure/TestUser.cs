namespace Bookstore.Api.IntegrationTests.Infrastructure;

public sealed record TestUser(int CustomerId, params string[] Roles)
{
    public static TestUser Customer(int id) => new(id);

    public static TestUser Admin { get; } = new(1, "admin");

    public string HeaderValue => $"{CustomerId};{string.Join(',', Roles)}";
}
