using Anthropic;
using Anthropic.Core;
using Anthropic.Models.Skills;

namespace Bookstore.Api.Reports;

public static class SkillUploader
{
    // File names keep the folder ("bookstore-report/SKILL.md"), so SKILL.md sits at the top of one enclosing folder.
    public static SkillCreateParams BuildCreateParams(string skillDirectory)
    {
        string root = Path.GetFullPath(skillDirectory);
        string folder = new DirectoryInfo(root).Name;
        List<BinaryContent> files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Select(path => new BinaryContent
            {
                Stream = File.OpenRead(path),
                FileName = folder + "/" + Path.GetRelativePath(root, path).Replace('\\', '/'),
            })
            .ToList();

        if (!files.Any(f => f.FileName == folder + "/SKILL.md"))
        {
            throw new InvalidOperationException($"{skillDirectory} has no SKILL.md at its top level.");
        }

        return new SkillCreateParams { Files = files };
    }

    public static async Task<Skill> UploadAsync(IAnthropicClient claude, string skillDirectory, CancellationToken cancellationToken)
    {
        SkillCreateParams parameters = BuildCreateParams(skillDirectory);
        try
        {
            return await claude.Skills.Create(parameters, cancellationToken);
        }
        finally
        {
            foreach (BinaryContent file in parameters.Files)
            {
                await file.Stream.DisposeAsync();
            }
        }
    }
}
