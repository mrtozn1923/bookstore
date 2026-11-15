using System.Text.Json;

namespace Bookstore.Prompts.Evaluation;

public sealed record TestCase<T>(string Id, string Input, T Expected, string Note = "");

public static class TestSet
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // One JSON object per line: {"id": "...", "input": "...", "expected": {...}, "note": "..."}
    public static IReadOnlyList<TestCase<T>> LoadJsonl<T>(string path) =>
        [.. File.ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<TestCase<T>>(line, Json)
                ?? throw new InvalidDataException($"Bad test case line in {path}"))];
}
