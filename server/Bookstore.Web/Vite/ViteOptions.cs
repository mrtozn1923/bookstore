namespace Bookstore.Web.Vite;

public sealed class ViteOptions
{
    public const string SectionName = "Vite";

    // Set only in Development: scripts then come from the Vite dev server
    public string? DevServerUrl { get; set; }

    public string OutputPath { get; set; } = "dist";
}
