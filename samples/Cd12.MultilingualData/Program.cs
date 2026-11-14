using Bookstore.Catalog.Localization;
using Bookstore.Catalog.Localization.Json;
using Microsoft.AspNetCore.Localization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

string cs = Environment.GetEnvironmentVariable("BOOKSTORE_DB")
    ?? "Server=localhost;Database=BookstoreDb;Trusted_Connection=True;TrustServerCertificate=True";

if (args.Contains("--api"))
{
    // dotnet run -- --api   (after a console run has created the demo data)
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
    builder.Services.AddDbContext<BookstoreContext>(o => o.UseSqlServer(cs));
    builder.Services.Configure<RequestLocalizationOptions>(o =>
    {
        o.SetDefaultCulture("tr").AddSupportedCultures("tr", "en", "en-GB").AddSupportedUICultures("tr", "en", "en-GB");
    });
    WebApplication app = builder.Build();
    app.UseRequestLocalization();

    app.MapGet("/api/categories", async (HttpContext http, BookstoreContext db, CancellationToken ct) =>
    {
        string culture = http.Features.Get<IRequestCultureFeature>()!.RequestCulture.UICulture.Name;
        string[] chain = CultureChain.Build(culture, "tr");

        List<CategoryDto> items = await db.Categories
            .Where(c => c.IsActive)
            .Localized(db.CategoryTranslations, chain)
            .ToListAsync(ct);

        http.Response.Headers.ContentLanguage = culture;
        http.Response.Headers.Vary = "Accept-Language";
        return Results.Ok(items);
    });
    app.Run();
    return;
}

// 1) Naive: one column per language
DbContextOptions<Bookstore.Catalog.Localization.Naive.NaiveContext> naiveOptions = new DbContextOptionsBuilder<Bookstore.Catalog.Localization.Naive.NaiveContext>()
    .UseSqlServer(cs.Replace("Database=", "Database=Naive_")).Options;
await using (var db = new Bookstore.Catalog.Localization.Naive.NaiveContext(naiveOptions))
{
    await db.Database.EnsureDeletedAsync();
    await db.Database.EnsureCreatedAsync();
    db.Categories.AddRange(
        new Bookstore.Catalog.Localization.Naive.Category { Code = "HISTORY", NameTr = "Tarih", NameEn = "History" },
        new Bookstore.Catalog.Localization.Naive.Category { Code = "LIGHT_SOUND", NameTr = "Işıklı ve sesli kitaplar" });
    await db.SaveChangesAsync();

    string lang = "en";
    var items = await db.Categories
        .Select(c => new { c.Code, Name = lang == "en" ? c.NameEn : c.NameTr })
        .ToListAsync();
    foreach (var i in items) Console.WriteLine($"{i.Code}: '{i.Name}'");
    await db.Database.EnsureDeletedAsync();
}

// 2) Translation table
DbContextOptions<BookstoreContext> options = new DbContextOptionsBuilder<BookstoreContext>().UseSqlServer(cs).Options;
await using (var setup = new BookstoreContext(options))
{
    await setup.Database.EnsureDeletedAsync();
    await setup.Database.EnsureCreatedAsync();
    static Category C(string code, params (string Culture, string Name)[] names) =>
        new() { Code = code, Translations = names.Select(n => new CategoryTranslation { Culture = n.Culture, Name = n.Name }).ToList() };
    setup.Categories.AddRange(
        C("HISTORY", ("tr", "Tarih"), ("en", "History")),
        C("COOKERY", ("tr", "Yemek kitapları"), ("en", "Cookbooks"), ("en-GB", "Cookery")),
        C("MEDIA", ("tr", "İletişim ve medya"), ("en", "Communication and media")),
        C("CLASSICS", ("tr", "Klasikler"), ("en", "Classics")),
        C("LIGHT_SOUND", ("tr", "Işıklı ve sesli kitaplar")),
        C("COMICS", ("tr", "Çizgi roman"), ("en", "Comics")));
    await setup.SaveChangesAsync();
}

await using (var db = new BookstoreContext(options))
{
    foreach (CategoryTranslation bad in new[]
    {
        new CategoryTranslation { CategoryId = 4, Culture = "de", Name = "" },
        new CategoryTranslation { CategoryId = 5, Culture = "en", Name = "History" }
    })
    {
        try
        {
            db.CategoryTranslations.Add(bad);
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql)
        {
            Console.WriteLine(sql.Errors[0].Message);
        }
        db.ChangeTracker.Clear();
    }

    foreach (string? requested in new[] { "en-GB", "de-DE", "tr-TR", null })
    {
        Console.WriteLine($"{requested ?? "(none)",-6} -> {string.Join(" -> ", CultureChain.Build(requested, "tr"))}");
    }

    Console.WriteLine(db.Categories.Where(c => c.IsActive)
        .Localized(db.CategoryTranslations, CultureChain.Build("en-GB", "tr")).ToQueryString());

    // 3) the opening question: search and sorting
    string term = "iletişim";
    int count = await db.CategoryTranslations
        .CountAsync(t => t.Culture == "tr" && t.Name.Contains(term));
    int found = await db.CategoryTranslations
        .CountAsync(t => t.Culture == "tr"
            && EF.Functions.Like(EF.Functions.Collate(t.Name, SqlCollation.For("tr")), "%" + term + "%"));
    Console.WriteLine($"Contains: {count}, Turkish collation: {found}");
    Console.WriteLine(db.CategoryTranslations.Where(t => t.Culture == "tr"
        && EF.Functions.Like(EF.Functions.Collate(t.Name, SqlCollation.For("tr")), "%" + term + "%")).ToQueryString());

    string[] collations = ["SQL_Latin1_General_CP1_CI_AS", "Latin1_General_100_CI_AS", "Turkish_CI_AS", "Turkish_100_CI_AS"];
    Console.WriteLine("Term      Latin1 Latin1_100 Turkish Turkish_100");
    foreach (string t in new[] { "iletişim", "İLETİŞİM", "ILETIŞIM", "KLASIKLER", "ışık", "IŞIK" })
    {
        List<int> counts = [];
        foreach (string coll in collations)
        {
            counts.Add(await db.CategoryTranslations
                .CountAsync(x => x.Culture == "tr" && EF.Functions.Like(EF.Functions.Collate(x.Name, coll), t + "%")));
        }
        Console.WriteLine($"{t,-9} {counts[0],-6} {counts[1],-10} {counts[2],-7} {counts[3]}");
    }

    List<string> byDefault = await db.CategoryTranslations.Where(t => t.Culture == "tr")
        .OrderBy(t => t.Name).Select(t => t.Name).ToListAsync();
    List<string> byTurkish = await db.CategoryTranslations.Where(t => t.Culture == "tr")
        .OrderBy(t => EF.Functions.Collate(t.Name, "Turkish_100_CI_AS")).Select(t => t.Name).ToListAsync();
    Console.WriteLine($"Default: {string.Join(", ", byDefault)}");
    Console.WriteLine($"Turkish: {string.Join(", ", byTurkish)}");
}

// 4) Option C: json column
DbContextOptions<JsonContext> jsonOptions = new DbContextOptionsBuilder<JsonContext>()
    .UseSqlServer(cs.Replace("Database=", "Database=Json_"), o => o.UseCompatibilityLevel(170)).Options;
await using (var jdb = new JsonContext(jsonOptions))
{
    await jdb.Database.EnsureDeletedAsync();
    await jdb.Database.EnsureCreatedAsync();
    string create = jdb.Database.GenerateCreateScript();
    Console.WriteLine(create[create.IndexOf("CREATE TABLE [InstitutionUnits]")..].Split(";")[0].Trim() + ";");
    jdb.InstitutionUnits.AddRange(
        new InstitutionUnit { Name = new LocalizedText { Tr = "Kütüphane", En = "Library" } },
        new InstitutionUnit { Name = new LocalizedText { Tr = "İdari İşler" } });
    await jdb.SaveChangesAsync();
    var q = jdb.InstitutionUnits.OrderBy(u => u.Id).Select(u => u.Name.En ?? u.Name.Tr);
    Console.WriteLine(q.ToQueryString());
    Console.WriteLine(string.Join(", ", await q.ToListAsync()));
    try
    {
        await jdb.Database.ExecuteSqlRawAsync("CREATE INDEX IX_InstitutionUnits_Name ON dbo.InstitutionUnits (Name)");
    }
    catch (SqlException ex)
    {
        Console.WriteLine(ex.Message);
    }
    await jdb.Database.EnsureDeletedAsync();
}
