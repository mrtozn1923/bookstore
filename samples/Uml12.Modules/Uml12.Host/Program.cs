using System.Globalization;
using System.Reflection;
using Bookstore.Billing;
using Bookstore.DistributorIntegration;
using Bookstore.Institutional;
using Bookstore.Institutional.Contracts;
using Bookstore.Notifications;
using Microsoft.Extensions.DependencyInjection;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
string mode = args.FirstOrDefault() ?? "run"; // run | wiring | missing

// Composition root: assembly connector'lar burada kuruluyor
ServiceCollection services = new();
services.AddInstitutionalModule();
if (mode != "missing")
{
    services.AddNotificationsModule();
}
services.AddDistributorIntegration();
services.AddBillingModule();

if (mode == "wiring")
{
    PrintWiring(services);
    return;
}

try
{
    await using ServiceProvider provider = services.BuildServiceProvider(
        new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    await using AsyncServiceScope scope = provider.CreateAsyncScope();
    Console.WriteLine("Approve #42 by Grace:");
    await scope.ServiceProvider.GetRequiredService<IOrderService>().ApproveAsync(42, approverId: 1);
    decimal invoice = await scope.ServiceProvider.GetRequiredService<InvoiceCalculator>()
        .MonthlyInvoiceAsync(institutionId: 1, 2026, 11);
    Console.WriteLine($"Billing: Sample School, 2026-11 invoice = {invoice}");
}
catch (AggregateException ex)
{
    Console.WriteLine(ex.InnerExceptions[0].Message);
}

static void PrintWiring(IServiceCollection services)
{
    // Her constructor parametresi bir required interface; onu karşılayan kayıt bir assembly connector
    foreach (ServiceDescriptor d in services.Where(d => d.ImplementationType is not null))
    {
        Type impl = d.ImplementationType!;
        ConstructorInfo ctor = impl.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();
        foreach (ParameterInfo p in ctor.GetParameters())
        {
            ServiceDescriptor? provider = services.LastOrDefault(s => s.ServiceType == p.ParameterType);
            string by = provider?.ImplementationType is { } t ? $"{t.Assembly.GetName().Name}.{t.Name}" : "MISSING";
            Console.WriteLine($"{impl.Assembly.GetName().Name,-23} {impl.Name,-17} requires {p.ParameterType.Name,-19} <- {by}");
        }
    }
}
