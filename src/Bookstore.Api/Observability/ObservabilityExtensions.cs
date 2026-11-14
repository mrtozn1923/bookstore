using System.Reflection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Bookstore.Observability;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddBookstoreObservability(this WebApplicationBuilder builder)
    {
        string version = typeof(ObservabilityExtensions).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

        builder.Services.AddOpenTelemetry()
            // Who is sending: these attributes are attached to every span, metric point and log record.
            // Code gives the defaults; OTEL_SERVICE_NAME / OTEL_RESOURCE_ATTRIBUTES, read last, override them.
            .ConfigureResource(resource => resource
                .AddService(serviceName: "bookstore-api", serviceVersion: version)
                .AddAttributes([new("deployment.environment.name", builder.Environment.EnvironmentName.ToLowerInvariant())])
                .AddEnvironmentVariableDetector())
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())
            .WithLogging()
            // One OTLP exporter for all three signals; the endpoint comes from OTEL_EXPORTER_OTLP_ENDPOINT.
            .UseOtlpExporter();

        return builder;
    }
}
