using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Catalog.Localization;

public static class CultureChain
{
    public static string[] Build(string? requested, string fallback)
    {
        List<string> chain = [];
        if (!string.IsNullOrWhiteSpace(requested))
        {
            for (CultureInfo c = CultureInfo.GetCultureInfo(requested); c.Name.Length > 0; c = c.Parent)
            {
                if (!chain.Contains(c.Name)) chain.Add(c.Name);
            }
        }
        if (!chain.Contains(fallback)) chain.Add(fallback);
        return [.. chain];
    }
}

public static class SqlCollation
{
    public static string For(string culture) =>
        culture.StartsWith("tr", StringComparison.OrdinalIgnoreCase)
            ? "Turkish_100_CI_AS"
            : "Latin1_General_100_CI_AS";
}

public static class CategoryQueries
{
    public static IQueryable<CategoryDto> Localized(
        this IQueryable<Category> categories,
        IQueryable<CategoryTranslation> translations,
        string[] chain)
    {
        // the chain has at most three links: en-GB -> en -> tr
        string c0 = chain[0];
        string c1 = chain.Length > 1 ? chain[1] : chain[0];
        string c2 = chain[^1];
        string collation = SqlCollation.For(c0);

        return categories
            .LeftJoin(translations.Where(t => t.Culture == c0),
                c => c.Id, t => t.CategoryId, (c, t0) => new { c, t0 })
            .LeftJoin(translations.Where(t => t.Culture == c1),
                x => x.c.Id, t => t.CategoryId, (x, t1) => new { x.c, x.t0, t1 })
            .LeftJoin(translations.Where(t => t.Culture == c2),
                x => x.c.Id, t => t.CategoryId, (x, t2) => new { x.c, x.t0, x.t1, t2 })
            .Select(x => new
            {
                x.c.Id,
                x.c.Code,
                Name = x.t0!.Name ?? x.t1!.Name ?? x.t2!.Name,
                Culture = x.t0 != null ? c0 : x.t1 != null ? c1 : x.t2 != null ? c2 : null
            })
            .OrderBy(x => EF.Functions.Collate(x.Name ?? x.Code, collation))
            .Select(x => new CategoryDto(x.Id, x.Code, x.Name ?? x.Code, x.Culture ?? "-"));
    }
}
