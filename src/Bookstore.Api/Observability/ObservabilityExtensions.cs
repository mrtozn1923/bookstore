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
                // Bookstore.Ordering, Bookstore.Notifications…: the spans we start ourselves
                .AddSource("Bookstore.*")
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation())
            .WithMetrics(metrics => metrics
                // Bookstore.Ordering, Bookstore.Inventory… and Polly's resilience metrics (retries, timeouts)
                .AddMeter("Bookstore.*", "Polly")
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                // Exemplars are off by default in .NET: keep one sampled measurement with its trace_id per bucket,
                // so a p99 spike on a dashboard leads to the trace of a slow request.
                .SetExemplarFilter(ExemplarFilterType.TraceBased)
                // GET /metrics for a Prometheus that scrapes (pull); OTLP below still pushes the same metrics.
                .AddPrometheusExporter())
            // The log body is the rendered message ("Payment for order 3826 failed"), not only the template;
            // the template's values still travel as attributes (OrderId=3826).
            .WithLogging(configureBuilder: null, configureOptions: options => options.IncludeFormattedMessage = true)
            // One OTLP exporter for all three signals; the endpoint comes from OTEL_EXPORTER_OTLP_ENDPOINT.
            .UseOtlpExporter();

        return builder;
    }
}
