// Checks a switches.json before it is shipped to the instances: the same file every instance will load at once,
// so it is treated like user input. Usage: dotnet run tools/switches-check/switches-check.cs -- <file>
// Exit code 1 on the first problem list, so the pipeline that ships the file stops.
using System.Text.Json;

const int MaxBytes = 4096;
string path = args.Length > 0 ? args[0] : "switches.json";
List<string> problems = [];

byte[] bytes = File.ReadAllBytes(path);
if (bytes.Length > MaxBytes)
{
    problems.Add($"file is {bytes.Length} bytes, the limit is {MaxBytes}");
}

try
{
    using JsonDocument doc = JsonDocument.Parse(bytes, new JsonDocumentOptions { AllowDuplicateProperties = false });
    CheckObject(doc.RootElement, "", new()
    {
        ["Payments"] = payments => CheckObject(payments, "Payments", new()
        {
            ["TimeoutSeconds"] = v => CheckInt(v, "Payments:TimeoutSeconds", 1, 30),
            ["CheckoutEnabled"] = v => CheckBool(v, "Payments:CheckoutEnabled")
        })
    });
}
catch (JsonException ex)
{
    problems.Add($"not valid JSON: {ex.Message}");
}

foreach (string problem in problems)
{
    Console.WriteLine($"ERROR {path}: {problem}");
}
Console.WriteLine(problems.Count == 0 ? $"OK {path}" : $"{problems.Count} problem(s)");
return problems.Count == 0 ? 0 : 1;

void CheckObject(JsonElement element, string name, Dictionary<string, Action<JsonElement>> known)
{
    if (element.ValueKind != JsonValueKind.Object)
    {
        problems.Add($"'{name}' must be an object");
        return;
    }
    foreach (JsonProperty property in element.EnumerateObject())
    {
        // Unknown keys are errors: a typo ("CheckoutEnable") would otherwise be ignored silently.
        if (known.TryGetValue(property.Name, out Action<JsonElement>? check))
        {
            check(property.Value);
        }
        else
        {
            problems.Add($"unknown key '{(name.Length > 0 ? name + ":" : "")}{property.Name}'");
        }
    }
}

void CheckInt(JsonElement value, string name, int min, int max)
{
    if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out int n))
    {
        problems.Add($"'{name}' must be a whole number, got {value.GetRawText()}");
    }
    else if (n < min || n > max)
    {
        problems.Add($"'{name}' must be between {min} and {max}, got {n}");
    }
}

void CheckBool(JsonElement value, string name)
{
    if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
    {
        problems.Add($"'{name}' must be true or false, got {value.GetRawText()}");
    }
}
