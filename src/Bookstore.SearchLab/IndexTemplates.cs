using System.Text.Json.Nodes;
using Bookstore.Search;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using HttpMethod = Elastic.Transport.HttpMethod;

namespace Bookstore.SearchLab;

// Turns search/books.index.json into an index template: every new books-v* index gets the same analysis and mapping.
public static class IndexTemplates
{
    public const string IndexPattern = "books-v*";

    public static async Task RunAsync(string[] args)
    {
        int replicas = args.Length > 1 && args[0] == "--replicas" ? int.Parse(args[1]) : 0;
        ElasticsearchClient client = LabClient.CreateAdmin();

        // Shard and replica counts depend on the environment: they live in a component template of their own.
        JsonObject environment = new()
        {
            ["template"] = new JsonObject
            {
                ["settings"] = new JsonObject { ["number_of_shards"] = 1, ["number_of_replicas"] = replicas }
            },
            ["_meta"] = new JsonObject { ["description"] = "Shard and replica counts for this environment" }
        };
        await PutAsync(client, "/_component_template/bookstore-env", environment);

        // The analysis and mapping come from the same file the tests use; only the environment settings are removed.
        JsonObject definition = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine("search", "books.index.json")))!.AsObject();
        JsonObject settings = definition["settings"]!.AsObject();
        settings.Remove("number_of_shards");
        settings.Remove("number_of_replicas");

        JsonObject template = new()
        {
            ["index_patterns"] = new JsonArray(IndexPattern),
            ["priority"] = 200,
            ["composed_of"] = new JsonArray("bookstore-env"),
            ["template"] = definition.DeepClone(),
            ["_meta"] = new JsonObject { ["source"] = "search/books.index.json" }
        };
        await PutAsync(client, $"/_index_template/{SearchClientFactory.BooksIndex}", template);

        // What would a new books-v* index look like? The simulate API answers without creating one.
        StringResponse simulated = await client.Transport.RequestAsync<StringResponse>(
            HttpMethod.POST, "/_index_template/_simulate_index/books-v0");
        JsonNode result = JsonNode.Parse(simulated.Body)!;
        JsonNode index = result["template"]!["settings"]!["index"]!;
        JsonObject titleFields = result["template"]!["mappings"]!["properties"]!["title"]!["fields"]!.AsObject();
        Console.WriteLine($"Simulated books-v0: shards {index["number_of_shards"]}, replicas {index["number_of_replicas"]}, " +
            $"title fields: {string.Join(", ", titleFields.Select(f => f.Key))}");
    }

    private static async Task PutAsync(ElasticsearchClient client, string path, JsonObject body)
    {
        StringResponse response = await client.Transport.RequestAsync<StringResponse>(
            HttpMethod.PUT, path, PostData.String(body.ToJsonString()));
        Console.WriteLine($"PUT {path}: HTTP {response.ApiCallDetails.HttpStatusCode} {response.Body}");
    }
}
