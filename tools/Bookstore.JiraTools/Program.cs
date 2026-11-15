using Bookstore.JiraTools;

return args switch
{
    ["lint-jql", var path] => LintFilters(path),
    _ => Usage(),
};

static int LintFilters(string path)
{
    int filters = 0, warnings = 0, notes = 0;
    foreach (var (name, jql) in FilterFile.Read(path))
    {
        filters++;
        IReadOnlyList<Finding> findings = JqlLinter.Check(jql);
        Console.WriteLine(findings.Count == 0 ? $"{name}: ok" : name);
        foreach (Finding f in findings)
        {
            Console.WriteLine($"  {f.Severity.ToString().ToLowerInvariant(),-4}  {f.RuleId}  {f.Message}");
            if (f.Severity == Severity.Warn) warnings++; else notes++;
        }
    }
    Console.WriteLine($"{filters} filters, {warnings} warnings, {notes} notes");
    return warnings > 0 ? 1 : 0;
}

static int Usage()
{
    Console.Error.WriteLine("usage: jiratools lint-jql <filters.txt>");
    return 2;
}
