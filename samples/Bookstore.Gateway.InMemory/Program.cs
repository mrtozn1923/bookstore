using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromMemory(
        [
            new RouteConfig
            {
                RouteId = "catalog-v1",
                ClusterId = "catalog",
                Match = new RouteMatch { Path = "/api/v1/books/{**rest}" },
                Transforms = [new Dictionary<string, string> { ["PathPattern"] = "/books/{**rest}" }],
            },
        ],
        [CatalogCluster("http://localhost:5301/")]);

var app = builder.Build();

app.MapReverseProxy();

if (app.Environment.IsDevelopment())
{
    // Points the catalog cluster to another address without a restart.
    app.MapPost("/admin/catalog-address", (string address, InMemoryConfigProvider provider) =>
    {
        IProxyConfig current = provider.GetConfig();
        provider.Update(current.Routes, [CatalogCluster(address)]);
        return Results.Ok(new { previousRevision = current.RevisionId, address });
    });
}

app.Run();

static ClusterConfig CatalogCluster(string address) => new()
{
    ClusterId = "catalog",
    Destinations = new Dictionary<string, DestinationConfig>
    {
        ["catalog-1"] = new() { Address = address },
    },
};
