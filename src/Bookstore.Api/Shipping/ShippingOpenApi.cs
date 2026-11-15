using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;

namespace Bookstore.Shipping;

// An example response in /openapi/v1.json: mock servers such as Prism return it instead of "string" and int.MinValue.
public static class ShippingOpenApi
{
    public static OpenApiOptions AddShippingExamples(this OpenApiOptions options) =>
        options.AddSchemaTransformer((schema, context, _) =>
        {
            if (context.JsonTypeInfo.Type == typeof(ShippingQuoteView))
            {
                schema.Examples =
                [
                    new JsonObject
                    {
                        ["quoteId"] = "q-0001",
                        ["city"] = "Ankara",
                        ["books"] = 2,
                        ["price"] = 49.90m,
                        ["currency"] = "TRY",
                        ["estimatedDays"] = 2
                    }
                ];
            }
            return Task.CompletedTask;
        });
}
