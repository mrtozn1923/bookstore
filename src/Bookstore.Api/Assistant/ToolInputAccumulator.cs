using System.Text;
using System.Text.Json;

namespace Bookstore.Api.Assistant;

// Collects input_json_delta fragments per content block index and parses them once the block stops.
public sealed class ToolInputAccumulator
{
    private readonly Dictionary<long, StringBuilder> _buffers = [];

    public void Append(long index, string partialJson)
    {
        if (!_buffers.TryGetValue(index, out StringBuilder? buffer))
        {
            buffer = new StringBuilder();
            _buffers[index] = buffer;
        }

        buffer.Append(partialJson);
    }

    // Never run a tool with input that did not parse: with eager_input_streaming the API does not validate it.
    public bool TryComplete(long index, out JsonElement input)
    {
        input = default;
        if (!_buffers.Remove(index, out StringBuilder? buffer))
        {
            return false;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(buffer.ToString());
            input = document.RootElement.Clone();
            return input.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
