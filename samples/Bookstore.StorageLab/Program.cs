using System.Diagnostics;
using System.Text.RegularExpressions;
using Bookstore.Notifications;
using Bookstore.Notifications.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

string command = args.FirstOrDefault() ?? "traversal";

if (command == "traversal")
{
    string root = Path.GetFullPath("files");
    LocalFileStore store = new(Options.Create(new LocalFileStoreOptions { RootPath = root }));
    string[] inputs = ["invoices/2026/BKS-2026-001042.pdf", "../appsettings.json", "covers/../../files-backup/db.bak", "/etc/passwd",
        "invoices/./2026//x.pdf"];
    foreach (string input in inputs)
    {
        // The naive version: combine, then check with StartsWith(root) and no trailing separator
        string naive = Path.GetFullPath(Path.Combine(root, input));
        string naiveVerdict = naive.StartsWith(root, StringComparison.Ordinal) ? "passes" : "blocked";
        string ours;
        try
        {
            await store.ExistsAsync(input);
            ours = "accepted";
        }
        catch (ArgumentException)
        {
            ours = "rejected";
        }

        Console.WriteLine($"{input,-36} naive check: {naiveVerdict,-8} LocalFileStore: {ours}");
    }

    return;
}

IConfiguration configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
ServiceCollection services = new();
services.AddSingleton(TimeProvider.System);
services.AddFileStore(configuration.GetSection("Notifications:FileStore"));
await using ServiceProvider provider = services.BuildServiceProvider();
await provider.EnsureFileStoreAsync(["http://localhost:4200"]);
IFileStore files = provider.GetRequiredService<IFileStore>();
using HttpClient http = new();
Console.WriteLine($"store: {files.GetType().Name}");

switch (command)
{
    case "roundtrip":
    {
        byte[] pdf = await File.ReadAllBytesAsync(args[1]);
        const string path = "invoices/2026/BKS-2026-001042.pdf";
        Stopwatch watch = Stopwatch.StartNew();
        StoredFile stored = await files.SaveAsync(path, new MemoryStream(pdf), "application/pdf");
        Console.WriteLine($"saved {stored.Path} ({stored.Length:N0} bytes) in {watch.ElapsedMilliseconds} ms, exists={await files.ExistsAsync(path)}");

        Uri url = await files.GetDownloadUrlAsync(path, TimeSpan.FromMinutes(5), "invoice-BKS-2026-001042.pdf");
        Console.WriteLine($"url: {Redact(url)}");
        using (HttpResponseMessage response = await http.GetAsync(url))
        {
            byte[] body = await response.Content.ReadAsByteArrayAsync();
            Console.WriteLine($"GET url -> {(int)response.StatusCode}, {body.Length:N0} bytes, same={body.AsSpan().SequenceEqual(pdf)}, {response.Content.Headers.ContentDisposition}");
        }

        // Change one character of the path: the signature no longer matches
        Uri tampered = new(url.ToString().Replace("001042", "001043"));
        using (HttpResponseMessage response = await http.GetAsync(tampered))
        {
            Console.WriteLine($"GET other invoice with the same signature -> {(int)response.StatusCode}");
        }

        Uri shortLived = await files.GetDownloadUrlAsync(path, TimeSpan.FromSeconds(1));
        await Task.Delay(TimeSpan.FromSeconds(2));
        using (HttpResponseMessage response = await http.GetAsync(shortLived))
        {
            Console.WriteLine($"GET after expiry -> {(int)response.StatusCode}");
        }

        Console.WriteLine($"delete -> {await files.DeleteAsync(path)}, delete again -> {await files.DeleteAsync(path)}");
        break;
    }
    case "upload":
    {
        IDirectUploadStore uploads = provider.GetRequiredService<IDirectUploadStore>();
        byte[] image = await File.ReadAllBytesAsync(args[1]);
        string path = $"uploads/covers/9780441172719/{Guid.CreateVersion7():N}";
        DirectUpload upload = await uploads.CreateUploadAsync(path, "image/jpeg", TimeSpan.FromMinutes(10));
        Console.WriteLine($"upload url: {Redact(upload.Url)}");
        Console.WriteLine($"headers: {string.Join(", ", upload.Headers.Select(h => $"{h.Key}: {h.Value}"))}");

        Console.WriteLine($"PUT image -> {await PutAsync(upload, image)}");
        Console.WriteLine($"PUT again with the same URL -> {await PutAsync(upload, image)}");
        using (HttpResponseMessage response = await http.GetAsync(upload.Url))
        {
            Console.WriteLine($"GET with the upload URL -> {(int)response.StatusCode}");
        }

        DirectUpload big = await uploads.CreateUploadAsync(path + "-big", "image/jpeg", TimeSpan.FromMinutes(10));
        Console.WriteLine($"PUT 12 MB of zeros -> {await PutAsync(big, new byte[12 * 1024 * 1024])}");
        await files.DeleteAsync(path);
        await files.DeleteAsync(path + "-big");
        break;
    }
}

async Task<string> PutAsync(DirectUpload upload, byte[] body)
{
    using HttpRequestMessage request = new(HttpMethod.Put, upload.Url) { Content = new ByteArrayContent(body) };
    foreach ((string name, string value) in upload.Headers)
    {
        if (!request.Headers.TryAddWithoutValidation(name, value))
        {
            request.Content.Headers.TryAddWithoutValidation(name, value);
        }
    }

    using HttpResponseMessage response = await http.SendAsync(request);
    return $"{(int)response.StatusCode} {response.ReasonPhrase}";
}

// Signatures are credentials for as long as they live: never log them
static string Redact(Uri url) => Regex.Replace(url.ToString(), "(sig|X-Amz-Signature|X-Amz-Credential)=[^&]+", "$1=<redacted>");
