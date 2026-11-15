using System.Text;
using Bookstore.SeoAudit;

// kullanım: dotnet run -- <test edilen adres> <production adresi> [rapor.md]
Uri target = new(args.Length > 0 ? args[0] : "http://127.0.0.1:52213/");
Uri site = new(args.Length > 1 ? args[1] : "https://bookstore.example/");
string? reportPath = args.Length > 2 ? args[2] : null;

Fetcher fetcher = new(target, site);
Auditor auditor = new(fetcher);
await auditor.RunAsync();

List<Finding> findings = [.. auditor.Findings.OrderBy(f => f.Severity).ThenBy(f => f.Rule).ThenBy(f => f.Page)];
int errors = findings.Count(f => f.Severity == Severity.Error);
int warnings = findings.Count - errors;

Console.WriteLine($"SEO audit {target} (as {site})");
Console.WriteLine($"sitemap URLs: {auditor.SitemapUrls}, pages reached by links: {auditor.CrawledPages}, requests: {fetcher.Requests}\n");
foreach (Finding f in findings)
    Console.WriteLine($"{(f.Severity == Severity.Error ? "ERROR" : "WARN"),-6}{f.Rule,-20}{f.Page,-34}{f.Detail}");
Console.WriteLine($"\n{errors} errors, {warnings} warnings");

// CI için: PR yorumuna ya da job summary'ye yazılacak Markdown rapor
if (reportPath is not null)
{
    StringBuilder md = new();
    md.AppendLine($"### SEO audit: {errors} errors, {warnings} warnings").AppendLine();
    md.AppendLine($"{auditor.SitemapUrls} sitemap URLs, {auditor.CrawledPages} pages reached by links.").AppendLine();
    if (findings.Count > 0)
    {
        md.AppendLine("| | Rule | Page | Detail |").AppendLine("|---|---|---|---|");
        foreach (Finding f in findings)
            md.AppendLine($"| {(f.Severity == Severity.Error ? "**error**" : "warning")} | `{f.Rule}` | `{f.Page}` | {f.Detail.Replace("|", "\\|")} |");
    }
    await File.WriteAllTextAsync(reportPath, md.ToString());
}

return errors > 0 ? 1 : 0;
