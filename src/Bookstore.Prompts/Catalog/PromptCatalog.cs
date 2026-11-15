using Bookstore.Prompts.Evaluation;
using Microsoft.Extensions.AI;

namespace Bookstore.Prompts.Catalog;

// Prompt files are embedded in the assembly: they change only through a build, never at run time
public sealed class PromptCatalog
{
    readonly Dictionary<string, PromptTemplate> templates;

    PromptCatalog(IEnumerable<PromptTemplate> templates) =>
        this.templates = templates.ToDictionary(t => t.Key);

    public static PromptCatalog Default { get; } = LoadEmbedded();

    public IReadOnlyCollection<PromptTemplate> All => templates.Values;

    public static PromptCatalog LoadEmbedded()
    {
        System.Reflection.Assembly assembly = typeof(PromptCatalog).Assembly;
        return new(assembly.GetManifestResourceNames()
            .Where(n => n.EndsWith(".prompt.md", StringComparison.Ordinal))
            .Select(n =>
            {
                using StreamReader reader = new(assembly.GetManifestResourceStream(n)!);
                return PromptTemplate.Parse(reader.ReadToEnd());
            }));
    }

    public PromptTemplate Get(string task, string version) =>
        templates.TryGetValue($"{task}@{version}", out PromptTemplate? template)
            ? template
            : throw new KeyNotFoundException($"No prompt {task}@{version} in the catalog");

    // Bridges a catalog entry to the PromptVersion the EvalRunner already uses
    public PromptVersion ToPromptVersion(
        string task, string version, string inputVariable, ChatResponseFormat? format = null,
        IReadOnlyDictionary<string, string>? fixedValues = null, bool escapeInput = true)
    {
        PromptTemplate template = Get(task, version);
        Dictionary<string, string> systemValues = new(fixedValues ?? new Dictionary<string, string>());
        return new PromptVersion(
            Task: task,
            Version: version,
            System: template.Fill(template.System, systemValues),
            BuildUser: input => template.Fill(template.User, new Dictionary<string, string> { [inputVariable] = input }, escapeInput),
            Format: format,
            Fingerprint: template.Fingerprint);
    }
}
