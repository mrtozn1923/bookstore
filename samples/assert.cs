#:project ../src/Bookstore.Ordering/Bookstore.Ordering.csproj
using Bookstore.Ordering;

// A line with a negative price: input nobody validated at the boundary
Cart cart = new(new Customer { Id = 1, Name = "Ada" },
    [new("9780441013593", "Dune", -120m, 1, false)],
    PaymentMethod.Card);

decimal discount = new CheckoutPolicy().Discount(cart, new DateOnly(2026, 11, 20));
Console.WriteLine($"Discount: {discount}");
