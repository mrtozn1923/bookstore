namespace Bookstore.Payments;

public static class PaymentsModule
{
    public static IServiceCollection AddPaymentsModule(this IServiceCollection services, IConfiguration configuration)
    {
        Uri baseAddress = new(configuration["Payments:BaseAddress"]
            ?? throw new InvalidOperationException("Payments:BaseAddress is missing."));
        TimeSpan timeout = TimeSpan.FromSeconds(configuration.GetValue("Payments:TimeoutSeconds", 3));

        // No retries on purpose: one attempt with a timeout, so every provider failure shows up in the SLI.
        services.AddHttpClient<IPaymentGateway, PaymentGatewayClient>(client =>
        {
            client.BaseAddress = baseAddress;
            client.Timeout = timeout;
        });
        return services;
    }
}
