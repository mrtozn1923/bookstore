using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace Bookstore.Prompts.Catalog;

// Front matter of a prompt file: the prompt is versioned together with its model and parameters
public sealed record PromptMetadata(
    string Task,
    string Version,
    string Model,
    string ModelDigest,
    float Temperature,
    int Seed,
    IReadOnlySet<string> Trusted);

public sealed partial class PromptTemplate
{
    PromptTemplate(PromptMetadata metadata, string system, string user, string fingerprint)
    {
        Metadata = metadata;
        System = system;
        User = user;
        Fingerprint = fingerprint;
    }

    public PromptMetadata Metadata { get; }
    public string System { get; }
    public string User { get; }
    public string Fingerprint { get; }
    public string Key => $"{Metadata.Task}@{Metadata.Version}";

    public IReadOnlySet<string> Variables =>
        Placeholder().Matches(System + User).Select(m => m.Groups[1].Value).ToHashSet();

    public static PromptTemplate Parse(string text)
    {
        string normalized = text.ReplaceLineEndings("\n");
        Match file = FileLayout().Match(normalized);
        if (!file.Success)
        {
            throw new FormatException("A prompt file needs front matter, a '# system' and a '# user' section.");
        }
        Dictionary<string, string> fields = file.Groups["meta"].Value
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split(':', 2))
            .ToDictionary(p => p[0].Trim(), p => p[1].Trim());

        PromptMetadata metadata = new(
            Task: fields["task"],
            Version: fields["version"],
            Model: fields["model"],
            ModelDigest: fields["model-digest"],
            Temperature: float.Parse(fields["temperature"], CultureInfo.InvariantCulture),
            Seed: int.Parse(fields["seed"], CultureInfo.InvariantCulture),
            Trusted: fields.TryGetValue("trusted", out string? trusted)
                ? trusted.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToHashSet()
                : []);
        string fingerprint = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)))[..12];
        return new(metadata, file.Groups["system"].Value.Trim(), file.Groups["user"].Value.Trim(), fingerprint);
    }

    public List<ChatMessage> Render(IReadOnlyDictionary<string, string> values) =>
        [new(ChatRole.System, Fill(System, values)), new(ChatRole.User, Fill(User, values))];

    public string Fill(string template, IReadOnlyDictionary<string, string> values, bool escapeUntrusted = true)
    {
        List<string> unknown = [.. values.Keys.Where(k => !Variables.Contains(k))];
        if (unknown.Count > 0)
        {
            throw new ArgumentException($"{Key} has no variable {string.Join(", ", unknown)}");
        }
        return Placeholder().Replace(template, m =>
        {
            string name = m.Groups[1].Value;
            if (!values.TryGetValue(name, out string? value))
            {
                throw new ArgumentException($"{Key} needs a value for {{{{{name}}}}}");
            }
            // Untrusted values cannot close or open the tags that delimit them
            return Metadata.Trusted.Contains(name) || !escapeUntrusted ? value : Escape(value);
        });
    }

    public static string Escape(string value) =>
        value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    [GeneratedRegex(@"\{\{([a-zA-Z][a-zA-Z0-9]*)\}\}")]
    private static partial Regex Placeholder();

    [GeneratedRegex(@"^---\n(?<meta>[\s\S]*?)\n---\n# system\n(?<system>[\s\S]*?)\n# user\n(?<user>[\s\S]*)$")]
    private static partial Regex FileLayout();
}
