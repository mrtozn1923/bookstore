using System.Diagnostics;

await PlaceOrderAsync("9780441172719", 2);

static async Task PlaceOrderAsync(string isbn, int quantity)
{
    await ReserveStockAsync(isbn, quantity);
    await Task.Yield();
}

static async Task ReserveStockAsync(string isbn, int quantity)
{
    await ChargeAsync(quantity * 349.90m);
    await Task.Yield();
}

static async Task ChargeAsync(decimal amount)
{
    // the live call stack while the payment is being charged, before any await has suspended
    Console.WriteLine(new StackTrace());
    await Task.Yield();
}
