using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;

namespace Bookstore.Web.Vite;

// <vite-entry src="src/main.ts" />: dev server scripts in Development, hashed files from the manifest otherwise
[HtmlTargetElement("vite-entry", TagStructure = TagStructure.WithoutEndTag)]
public sealed class ViteEntryTagHelper(ViteManifest manifest, IOptions<ViteOptions> options, HtmlEncoder encoder) : TagHelper
{
    public required string Src { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null;
        string? devServer = options.Value.DevServerUrl?.TrimEnd('/');

        if (!string.IsNullOrEmpty(devServer))
        {
            output.Content.AppendHtml(Script($"{devServer}/@vite/client"));
            output.Content.AppendHtml(Script($"{devServer}/{Src}"));
            return;
        }

        ViteChunk entry = manifest.GetEntry(Src);
        IReadOnlyList<ViteChunk> imported = manifest.GetImportedChunks(entry);
        string basePath = $"/{options.Value.OutputPath}/";

        foreach (string css in imported.SelectMany(chunk => chunk.Css ?? []).Concat(entry.Css ?? []).Distinct())
        {
            output.Content.AppendHtml($"<link rel=\"stylesheet\" href=\"{encoder.Encode(basePath + css)}\">\n");
        }
        output.Content.AppendHtml(Script(basePath + entry.File));
        foreach (ViteChunk chunk in imported)
        {
            output.Content.AppendHtml($"<link rel=\"modulepreload\" href=\"{encoder.Encode(basePath + chunk.File)}\">\n");
        }
    }

    private string Script(string src) => $"<script type=\"module\" src=\"{encoder.Encode(src)}\"></script>\n";
}
