using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;

namespace Bookstore.Api.Tests;

public static class TestMeters
{
    public static IMeterFactory Factory { get; } =
        new ServiceCollection().AddMetrics().BuildServiceProvider().GetRequiredService<IMeterFactory>();
}
