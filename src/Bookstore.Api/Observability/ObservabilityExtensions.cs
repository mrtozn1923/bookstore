using System.Reflection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace Bookstore.Observability;

public static class ObservabilityExtensions
{
    // Metrics only: the SLIs of this series come from ASP.NET Core's built-in http.server.request.duration
    // histogram. Setting up traces, logs and the Collector is the Observability series' job.
    public static WebApplicationBuilder AddBookstoreObservability(this WebApplicationBuilder builder)
    {
        string version = typeof(ObservabilityExtensions).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: "bookstore-api", serviceVersion: version)
                .AddAttributes([new("deployment.environment.name", builder.Environment.EnvironmentName.ToLowerInvariant())])
                .AddEnvironmentVariableDetector())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                // ASP.NET Core's default boundaries plus 0.8: the checkout latency SLO counts requests under 800 ms,
                // and a histogram can only count exactly up to one of its bucket boundaries.
                .AddView("http.server.request.duration", new ExplicitBucketHistogramConfiguration
                {
                    Boundaries = [0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 0.8, 1, 2.5, 5, 7.5, 10]
                })
                // Metrics only. OTEL_EXPORTER_OTLP_ENDPOINT / _PROTOCOL decide where they go (here: Prometheus' OTLP
                // receiver). UseOtlpExporter() would also send logs there, and Prometheus answers those with 404.
                .AddOtlpExporter());

        return builder;
    }
}
