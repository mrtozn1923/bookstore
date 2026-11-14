using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Bookstore.Gateway.Observability;

public static class GatewayTelemetry
{
    public static IServiceCollection AddGatewayTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        OpenTelemetryBuilder otel = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("bookstore-gateway"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                // YARP's own spans: proxy.forwarder per proxied request, plus active health check probes.
                .AddSource("Yarp.ReverseProxy"))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation());

        // Export only when a collector is configured (OTEL_EXPORTER_OTLP_ENDPOINT).
        if (!string.IsNullOrEmpty(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            otel.UseOtlpExporter();
        }
        return services;
    }
}
