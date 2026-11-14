using Bookstore.Institutional.Settings;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

string connectionString = Environment.GetEnvironmentVariable("BOOKSTORE_DB")
    ?? "Server=localhost;Database=BookstoreDb;Trusted_Connection=True;TrustServerCertificate=True";

const int School = 1, Library = 2;   // Sample School, Sample Library
const int Ada = 1, Linus = 4, Grace = 5;
const int Admin = 99;                // Bookstore corporate sales

// 1) The opening: EF Core adds IS NOT NULL filters to the unique index
SqlConnectionStringBuilder naiveCs = new(connectionString);
naiveCs.InitialCatalog += "_Naive";
await using (NaiveSettingsContext naive = new(new DbContextOptionsBuilder<NaiveSettingsContext>().UseSqlServer(naiveCs.ConnectionString).Options))
{
    await naive.Database.EnsureDeletedAsync();
    await naive.Database.EnsureCreatedAsync();
    Console.WriteLine(naive.Database.GenerateCreateScript().Split('\n').First(l => l.Contains("CREATE UNIQUE INDEX")).Trim());
    foreach (string v in new[] { "30", "31" })
    {
        await naive.Database.ExecuteSqlRawAsync(
            "INSERT SettingValues ([Key], Scope, InstitutionId, UserId, Value) VALUES ('invoice.due-days', 1, NULL, NULL, {0})", v);
        Console.WriteLine("inserted");
    }
    await naive.Database.EnsureDeletedAsync();
}

DbContextOptions<BookstoreContext> options = new DbContextOptionsBuilder<BookstoreContext>().UseSqlServer(connectionString).Options;
await using (BookstoreContext setup = new(options))
{
    await setup.Database.EnsureDeletedAsync();
    await setup.Database.EnsureCreatedAsync();
    Console.WriteLine(setup.Database.GenerateCreateScript().Split('\n').First(l => l.Contains("CREATE UNIQUE INDEX")).Trim());
    setup.SettingDefinitions.AddRange(
        new SettingDefinition { Key = "invoice.due-days", Type = SettingType.Int, DefaultValue = "30", LowestScope = SettingScope.Institution, Min = 0, Max = 180 },
        new SettingDefinition { Key = "notifications.email", Type = SettingType.Bool, DefaultValue = "true", LowestScope = SettingScope.User });
    await setup.SaveChangesAsync();

    // 2) Untyped key-value: the error appears at read time
    await setup.Database.ExecuteSqlRawAsync(
        "CREATE TABLE InstitutionSettingsEav (InstitutionId int NOT NULL, [Key] varchar(100) NOT NULL, Value nvarchar(400) NOT NULL, PRIMARY KEY (InstitutionId, [Key]));" +
        "INSERT InstitutionSettingsEav VALUES (2, 'InvoiceDueDays', N'45 days');");
    try
    {
        string raw = await setup.Database
            .SqlQueryRaw<string>("SELECT Value FROM InstitutionSettingsEav WHERE InstitutionId = 2 AND [Key] = 'InvoiceDueDays'")
            .SingleAsync();
        int dueDays = int.Parse(raw);
        Console.WriteLine(dueDays);
    }
    catch (FormatException ex)
    {
        Console.WriteLine($"FormatException: {ex.Message}");
    }

    // 3) With HasFilter(null) the index covers global and institution rows too
    string[] attempts =
    [
        "INSERT SettingValues ([Key], Scope, InstitutionId, UserId, Value) VALUES ('invoice.due-days', 1, NULL, NULL, '30')",
        "INSERT SettingValues ([Key], Scope, InstitutionId, UserId, Value) VALUES ('invoice.due-days', 1, NULL, NULL, '31')",
        "INSERT SettingValues ([Key], Scope, InstitutionId, UserId, Value) VALUES ('invoice.due-days', 2, 2, NULL, '45')",
        "INSERT SettingValues ([Key], Scope, InstitutionId, UserId, Value) VALUES ('invoice.due-days', 2, 2, NULL, '50')",
        "INSERT SettingValues ([Key], Scope, InstitutionId, UserId, Value) VALUES ('notifications.email', 3, 2, NULL, 'false')"
    ];
    foreach (string sql in attempts)
    {
        try
        {
            await setup.Database.ExecuteSqlRawAsync(sql);
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"SqlException {ex.Number}: {ex.Message.Split('\n')[0]}");
        }
    }
    await setup.Database.ExecuteSqlRawAsync("DELETE SettingValues");
}

// 4) Resolution, validation and audit
QueryCounter counter = new();
DbContextOptions<BookstoreContext> counted = new DbContextOptionsBuilder<BookstoreContext>()
    .UseSqlServer(connectionString).AddInterceptors(counter).Options;

await using (BookstoreContext db = new(counted))
{
    SettingsService settings = new(db, new MemoryCache(new MemoryCacheOptions()), TimeProvider.System);
    await settings.SetAsync(SettingKeys.InvoiceDueDays, 45, SettingScope.Institution, Library, null, Admin, "Contract amendment 2026-03");
    await settings.SetAsync(SettingKeys.EmailNotifications, false, SettingScope.User, Library, Linus, Linus, "User preference");

    foreach ((string name, int institution, int user) in new[] { ("library/linus", Library, Linus), ("library/grace", Library, Grace), ("school/ada", School, Ada) })
    {
        (int days, SettingScope daysFrom) = await settings.GetAsync(SettingKeys.InvoiceDueDays, institution, user);
        (bool email, SettingScope emailFrom) = await settings.GetAsync(SettingKeys.EmailNotifications, institution, user);
        Console.WriteLine($"{name,-15}invoice.due-days={days} ({daysFrom}), notifications.email={email} ({emailFrom})");
    }
    try
    {
        await settings.SetAsync(SettingKeys.InvoiceDueDays, 10, SettingScope.User, Library, Linus, Linus, "Personal preference");
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine(ex.Message);
    }
    try
    {
        await settings.SetAsync(SettingKeys.InvoiceDueDays, 400, SettingScope.Institution, Library, null, Admin, "Typo");
    }
    catch (ArgumentOutOfRangeException ex)
    {
        Console.WriteLine(ex.Message);
    }
}

// 5) Cache: a fresh service, then a write that invalidates one entry
await using (BookstoreContext db = new(counted))
{
    SettingsService settings = new(db, new MemoryCache(new MemoryCacheOptions()), TimeProvider.System);
    counter.Count = 0;
    await settings.GetAsync(SettingKeys.InvoiceDueDays, Library, Linus);
    Console.WriteLine($"first read: {counter.Count} queries");
    counter.Count = 0;
    for (int i = 0; i < 100; i++) await settings.GetAsync(SettingKeys.InvoiceDueDays, Library, Linus);
    Console.WriteLine($"next 100 reads: {counter.Count} queries");
    await settings.SetAsync(SettingKeys.InvoiceDueDays, 60, SettingScope.Institution, Library, null, Admin, "Renewal 2026-11");
    counter.Count = 0;
    (int after, _) = await settings.GetAsync(SettingKeys.InvoiceDueDays, Library, Linus);
    Console.WriteLine($"after write: {counter.Count} queries, invoice.due-days={after}");

    foreach (SettingChange c in await db.SettingChanges.AsNoTracking().OrderBy(c => c.Id).ToListAsync())
    {
        Console.WriteLine($"{c.Key,-20} {c.Scope,-11} inst={c.InstitutionId} user={(c.UserId?.ToString() ?? "-")} {(c.OldValue ?? "null")} -> {c.NewValue} by {c.ChangedBy}: {c.Reason}");
    }
}
