using Polly;
using Polly.Fallback;
using Polly.Registry;

namespace Bookstore.Shipping;

// Decorator: when the carrier fails, checkout continues with a flat tariff instead of an error.
public sealed class ResilientShippingCarrier(
    [FromKeyedServices(ResilientShippingCarrier.InnerKey)] IShippingCarrier inner,
    ResiliencePipelineProvider<string> pipelines) : IShippingCarrier
{
    public const string InnerKey = "carrier-direct";
    public const string PipelineName = "shipping-quote";

    public static readonly ShippingQuote FlatTariff = new("FLAT", 79.90m, "TRY", 3) { IsFlatRate = true };

    public async Task<ShippingQuote> GetQuoteAsync(ShippingQuoteRequest request, CancellationToken cancellationToken) =>
        await pipelines.GetPipeline<ShippingQuote>(PipelineName)
            .ExecuteAsync(async token => await inner.GetQuoteAsync(request, token), cancellationToken);

    public static void Configure(ResiliencePipelineBuilder<ShippingQuote> builder, ILogger logger) =>
        builder.AddFallback(new FallbackStrategyOptions<ShippingQuote>
        {
            // Only outages fall back; a contract violation must stay an error and raise an alert.
            ShouldHandle = new PredicateBuilder<ShippingQuote>()
                .Handle<ShippingCarrierException>(ex => ex.Kind is ShippingFailureKind.Transient or ShippingFailureKind.Unavailable),
            FallbackAction = _ => Outcome.FromResultAsValueTask(FlatTariff),
            OnFallback = args =>
            {
                logger.LogWarning(args.Outcome.Exception, "Carrier quote failed, using the flat tariff");
                return default;
            }
        });
}
