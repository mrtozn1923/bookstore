#:package ModelContextProtocol
#:property PublishAot=false
// Try the Bookstore MCP server yourself before Copilot does: start it, list tools and annotations, call two tools.
// The API must be running. From the repository root: dotnet run eng/mcp-smoke.cs
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

string apiUrl = Environment.GetEnvironmentVariable("BOOKSTORE_API_URL") ?? "http://localhost:5080";

await using McpClient client = await McpClient.CreateAsync(new StdioClientTransport(new()
{
    Name = "bookstore",
    Command = "dotnet",
    Arguments = ["run", "--project", "src/Bookstore.Mcp", "--no-build"],
    EnvironmentVariables = new Dictionary<string, string?> { ["BOOKSTORE_API_URL"] = apiUrl },
}));

Console.WriteLine($"server: {client.ServerInfo?.Name} {client.ServerInfo?.Version}, protocol {client.NegotiatedProtocolVersion}");
foreach (McpClientTool tool in await client.ListToolsAsync())
{
    // Copilot code review only uses tools with readOnlyHint = true.
    Console.WriteLine($"  {tool.Name,-18} readOnlyHint={tool.ProtocolTool.Annotations?.ReadOnlyHint}");
}

await Call("search_books", new() { ["query"] = "Austen" });
await Call("get_book", new() { ["isbn"] = "9780000000000" });

async Task Call(string name, Dictionary<string, object?> arguments)
{
    CallToolResult result = await client.CallToolAsync(name, arguments);
    string text = string.Join("\n", result.Content.OfType<TextContentBlock>().Select(c => c.Text));
    Console.WriteLine($"\n{name}({string.Join(", ", arguments.Select(a => $"{a.Key}: {a.Value}"))}) isError={result.IsError ?? false}\n{text}");
}
