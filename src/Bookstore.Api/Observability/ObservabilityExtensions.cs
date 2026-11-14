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
                .AddHttpClientInstrumentation())
            // OTEL_EXPORTER_OTLP_ENDPOINT / _PROTOCOL decide where the metrics go (here: Prometheus' OTLP receiver).
            .UseOtlpExporter();

        return builder;
    }
}
