using System.Globalization;
using System.Net.Http.Json;
using Bookstore.Api.Concurrency;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

string connectionString = Environment.GetEnvironmentVariable("BOOKSTORE_DB")
    ?? "Server=localhost;Database=BookstoreDb;Trusted_Connection=True;TrustServerCertificate=True";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:5320");
builder.Services.AddDbContext<BookstoreContext>(o => o.UseSqlServer(connectionString));
builder.Services.AddProblemDetails();
builder.Services.AddCors(o => o.AddPolicy("portal", p => p
    .WithOrigins("https://bookstore.example.com")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders("ETag")));

WebApplication app = builder.Build();
app.UseCors("portal");

await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
{
    BookstoreContext setup = scope.ServiceProvider.GetRequiredService<BookstoreContext>();
    await setup.Database.EnsureDeletedAsync();
    await setup.Database.EnsureCreatedAsync();
    // Sample School (institution 1): Ada in Science (unit 7)
    setup.InstitutionUsers.Add(new InstitutionUser
    {
        InstitutionId = 1, FullName = "Ada Lovelace", Phone = "+90 212 555 0101", SpendingLimit = 85000.00m, InstitutionUnitId = 7
    });
    await setup.SaveChangesAsync();
}

app.MapPut("/naive/institution-users/{id:int}", async (int id, UpdateInstitutionUserRequest request,
    BookstoreContext db, CancellationToken ct) =>
{
    InstitutionUser? user = await db.InstitutionUsers.SingleOrDefaultAsync(u => u.Id == id, ct);
    if (user is null)
    {
        return Results.NotFound();
    }

    user.FullName = request.FullName;
    user.Phone = request.Phone;
    user.SpendingLimit = request.SpendingLimit;
    user.InstitutionUnitId = request.InstitutionUnitId;
    await db.SaveChangesAsync(ct);
    return Results.Ok(InstitutionUserDto.From(user));
});

app.MapGet("/institution-users/{id:int}", async (int id, HttpContext http, BookstoreContext db, CancellationToken ct) =>
{
    InstitutionUser? user = await db.InstitutionUsers.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id, ct);
    if (user is null)
    {
        return Results.NotFound();
    }

    string etag = ETags.From(user.Version);
    if (http.Request.Headers.IfNoneMatch.ToString() == etag)
    {
        return Results.StatusCode(StatusCodes.Status304NotModified);
    }

    http.Response.Headers.ETag = etag;
    return Results.Ok(InstitutionUserDto.From(user));
});

app.MapPut("/institution-users/{id:int}", async (int id, UpdateInstitutionUserRequest request, HttpContext http,
    BookstoreContext db, CancellationToken ct) =>
{
    IfMatch ifMatch = ETags.ParseIfMatch(http.Request);
    if (ifMatch.Kind == IfMatchKind.Missing)
    {
        return Results.Problem(statusCode: StatusCodes.Status428PreconditionRequired,
            title: "If-Match header is required",
            detail: "GET the institution user first and send its ETag in If-Match.");
    }

    InstitutionUser? user = await db.InstitutionUsers.SingleOrDefaultAsync(u => u.Id == id, ct);
    if (user is null)
    {
        return Results.NotFound();
    }

    if (ifMatch.Kind == IfMatchKind.Invalid)
    {
        return PreconditionFailed(user);
    }
    if (ifMatch.Kind == IfMatchKind.Version)
    {
        // the version the client saw becomes UPDATE ... WHERE Version = @original
        db.Entry(user).Property(u => u.Version).OriginalValue = ifMatch.Version!;
    }

    user.FullName = request.FullName;
    user.Phone = request.Phone;
    user.SpendingLimit = request.SpendingLimit;
    user.InstitutionUnitId = request.InstitutionUnitId;

    try
    {
        await db.SaveChangesAsync(ct);
    }
    catch (DbUpdateConcurrencyException ex)
    {
        EntityEntry entry = ex.Entries.Single();
        PropertyValues? databaseValues = await entry.GetDatabaseValuesAsync(ct);
        if (databaseValues is null)
        {
            return Results.NotFound();
        }
        entry.OriginalValues.SetValues(databaseValues);
        entry.CurrentValues.SetValues(databaseValues);
        return PreconditionFailed((InstitutionUser)entry.Entity);
    }

    http.Response.Headers.ETag = ETags.From(user.Version);
    return Results.Ok(InstitutionUserDto.From(user));
});

static IResult PreconditionFailed(InstitutionUser current) =>
    Results.Problem(statusCode: StatusCodes.Status412PreconditionFailed,
        title: "The institution user was changed by someone else",
        detail: "Compare your changes with the current values and retry with the new ETag.",
        extensions: new Dictionary<string, object?>
        {
            ["currentETag"] = ETags.From(current.Version),
            ["current"] = InstitutionUserDto.From(current)
        });

await app.StartAsync();
using HttpClient client = new() { BaseAddress = new Uri("http://127.0.0.1:5320") };

async Task<(InstitutionUserDto, string)> Get(string who, string? ifNoneMatch = null, string label = "")
{
    using HttpRequestMessage req = new(HttpMethod.Get, "/institution-users/1");
    if (ifNoneMatch is not null) req.Headers.TryAddWithoutValidation("If-None-Match", ifNoneMatch);
    using HttpResponseMessage res = await client.SendAsync(req);
    string etag = res.Headers.ETag?.ToString() ?? "";
    Console.WriteLine(ifNoneMatch is null
        ? $"{who} GET: {(int)res.StatusCode} ETag: {etag}"
        : $"{who} GET If-None-Match {label}: {(int)res.StatusCode}");
    InstitutionUserDto dto = res.StatusCode == System.Net.HttpStatusCode.OK
        ? (await res.Content.ReadFromJsonAsync<InstitutionUserDto>())!
        : null!;
    return (dto, etag);
}

async Task Put(string who, string? ifMatch, UpdateInstitutionUserRequest body)
{
    using HttpRequestMessage req = new(HttpMethod.Put, "/institution-users/1") { Content = JsonContent.Create(body) };
    if (ifMatch is not null) req.Headers.TryAddWithoutValidation("If-Match", ifMatch);
    using HttpResponseMessage res = await client.SendAsync(req);
    Console.WriteLine($"{who} PUT: {(int)res.StatusCode} ETag: {res.Headers.ETag} {await res.Content.ReadAsStringAsync()}");
}

// Naive: rowversion in the model, but the endpoint re-reads the row
(InstitutionUserDto a, _) = await Get("Grace");
(InstitutionUserDto b, _) = await Get("Linus");
await client.PutAsJsonAsync("/naive/institution-users/1", b.ToRequest() with { SpendingLimit = 92000.00m });
await client.PutAsJsonAsync("/naive/institution-users/1", a.ToRequest() with { Phone = "+90 212 555 0199" });
InstitutionUserDto final = (await client.GetFromJsonAsync<InstitutionUserDto>("/institution-users/1"))!;
Console.WriteLine($"Naive result: {final.SpendingLimit} / {final.Phone}");
Console.WriteLine("---");

// Reset the card and run the same scenario with ETag + If-Match
await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
{
    BookstoreContext db = scope.ServiceProvider.GetRequiredService<BookstoreContext>();
    await db.InstitutionUsers.ExecuteUpdateAsync(s => s
        .SetProperty(u => u.Phone, "+90 212 555 0101").SetProperty(u => u.SpendingLimit, 85000.00m));
}

(InstitutionUserDto graceView, string graceETag) = await Get("Grace");
(InstitutionUserDto linusView, string linusETag) = await Get("Linus");

await Put("Linus", linusETag, linusView.ToRequest() with { SpendingLimit = 92000.00m });
await Put("Grace", graceETag, graceView.ToRequest() with { Phone = "+90 212 555 0199" });
await Put("Grace", null, graceView.ToRequest() with { Phone = "+90 212 555 0199" });
await Put("Grace", "W/" + graceETag, graceView.ToRequest() with { Phone = "+90 212 555 0199" });
Console.WriteLine("---");

await Get("Grace", graceETag, "old");
(InstitutionUserDto current, string currentETag) = await Get("Grace");
await Get("Grace", currentETag, "current");
await Put("Grace", currentETag, current.ToRequest() with { Phone = "+90 212 555 0199" });

await app.StopAsync();
