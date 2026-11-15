#:property PublishAot=false
// Minimal Agent Client Protocol client: starts `copilot --acp`, opens a session, sends one prompt
// and prints what the agent streams back. From the repository root: dotnet run eng/acp-client.cs "<prompt>"
// Copilot CLI must be signed in or have a BYOK provider (COPILOT_PROVIDER_* variables).
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

string prompt = args.FirstOrDefault() ?? "How many projects does Bookstore.slnx list? Answer in one sentence.";
string agent = Environment.GetEnvironmentVariable("COPILOT_CLI") ?? "copilot";

ProcessStartInfo start = new(agent, "--acp --stdio --available-tools=view,glob")
{
    RedirectStandardInput = true,
    RedirectStandardOutput = true,
    UseShellExecute = false,
};
using Process copilot = Process.Start(start)!;
int nextId = 1;

JsonNode initialize = await Request("initialize", new JsonObject
{
    ["protocolVersion"] = 1,
    ["clientCapabilities"] = new JsonObject { ["fs"] = new JsonObject { ["readTextFile"] = false, ["writeTextFile"] = false } },
});
Console.WriteLine($"agent: {initialize["agentInfo"]?["name"]} {initialize["agentInfo"]?["version"]}, protocol {initialize["protocolVersion"]}");
Console.WriteLine($"auth methods: {string.Join(", ", initialize["authMethods"]?.AsArray().Select(m => m?["id"]?.ToString()) ?? [])}");

JsonNode session = await Request("session/new", new JsonObject { ["cwd"] = Environment.CurrentDirectory, ["mcpServers"] = new JsonArray() });
string sessionId = session["sessionId"]!.ToString();
Console.WriteLine($"session: {sessionId[..8]}...");

JsonNode result = await Request("session/prompt", new JsonObject
{
    ["sessionId"] = sessionId,
    ["prompt"] = new JsonArray(new JsonObject { ["type"] = "text", ["text"] = prompt }),
});
Console.WriteLine($"\nstop reason: {result["stopReason"]}");
copilot.StandardInput.Close();
await copilot.WaitForExitAsync();

// Sends a JSON-RPC request and handles notifications and agent requests until the response arrives.
async Task<JsonNode> Request(string method, JsonObject parameters)
{
    int id = nextId++;
    JsonObject message = new() { ["jsonrpc"] = "2.0", ["id"] = id, ["method"] = method, ["params"] = parameters };
    await copilot.StandardInput.WriteLineAsync(message.ToJsonString());
    await copilot.StandardInput.FlushAsync();

    while (await copilot.StandardOutput.ReadLineAsync() is string line)
    {
        JsonNode? incoming = JsonNode.Parse(line);
        if (incoming?["id"]?.GetValue<int>() == id && incoming["method"] is null)
        {
            return incoming["result"] ?? throw new InvalidOperationException(incoming["error"]?.ToJsonString());
        }
        if (incoming?["method"]?.ToString() == "session/update")
        {
            JsonNode? update = incoming["params"]?["update"];
            switch (update?["sessionUpdate"]?.ToString())
            {
                case "agent_message_chunk":
                    Console.Write(update["content"]?["text"]);
                    break;
                case "tool_call":
                    Console.WriteLine($"\n[tool] {update["title"]} ({update["kind"]})");
                    break;
            }
        }
        else if (incoming?["method"]?.ToString() == "session/request_permission")
        {
            // This client never grants permissions: it answers every request with the reject option.
            string optionId = incoming["params"]?["options"]?.AsArray()
                .FirstOrDefault(o => o?["kind"]?.ToString().StartsWith("reject") == true)?["optionId"]?.ToString() ?? "reject";
            JsonObject answer = new()
            {
                ["jsonrpc"] = "2.0",
                ["id"] = incoming["id"]!.DeepClone(),
                ["result"] = new JsonObject { ["outcome"] = new JsonObject { ["outcome"] = "selected", ["optionId"] = optionId } },
            };
            await copilot.StandardInput.WriteLineAsync(answer.ToJsonString());
            await copilot.StandardInput.FlushAsync();
        }
    }
    throw new InvalidOperationException("the agent closed the connection");
}
