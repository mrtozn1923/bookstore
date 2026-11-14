using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace Bookstore.OrderImport.Worker;

public sealed class OrderFileWatcher(
    OrderFileImporter importer,
    IOptions<OrderImportOptions> options,
    TimeProvider time,
    ILogger<OrderFileWatcher> logger) : BackgroundService
{
    private readonly OrderImportOptions _options = options.Value;

    // Path -> time of the last event. Many events for one file collapse into one entry.
    private readonly ConcurrentDictionary<string, DateTimeOffset> _pending = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string inbox = Path.GetFullPath(_options.InboxPath);
        Directory.CreateDirectory(inbox);
        Directory.CreateDirectory(_options.ProcessedPath);

        using var watcher = new FileSystemWatcher(inbox, _options.Filter)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
            InternalBufferSize = 64 * 1024
        };
        watcher.Created += (_, e) => Touch(e.FullPath);
        watcher.Changed += (_, e) => Touch(e.FullPath);
        watcher.Renamed += (_, e) => Touch(e.FullPath);
        watcher.Error += (_, e) =>
        {
            logger.LogWarning(e.GetException(), "Watcher error, rescanning {Inbox}", _options.InboxPath);
            Rescan(inbox);
        };
        watcher.EnableRaisingEvents = true;

        Rescan(inbox); // files that arrived while the service was down
        logger.LogInformation("Watching {Inbox} for {Filter}", _options.InboxPath, _options.Filter);

        DateTimeOffset lastRescan = time.GetUtcNow();
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(250), time);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (time.GetUtcNow() - lastRescan >= _options.RescanInterval)
            {
                Rescan(inbox);
                lastRescan = time.GetUtcNow();
            }
            await ProcessDueFilesAsync(stoppingToken);
        }
    }

    private void Touch(string path) => _pending[path] = time.GetUtcNow();

    private void Rescan(string inbox)
    {
        foreach (string path in Directory.EnumerateFiles(inbox, _options.Filter))
        {
            _pending.TryAdd(path, DateTimeOffset.MinValue);
        }
    }

    private async Task ProcessDueFilesAsync(CancellationToken ct)
    {
        DateTimeOffset now = time.GetUtcNow();
        foreach (var (path, lastEvent) in _pending)
        {
            if (now - lastEvent < _options.QuietPeriod)
            {
                continue; // still receiving events: wait for the file to settle
            }
            if (!File.Exists(path))
            {
                _pending.TryRemove(path, out _);
                continue;
            }

            try
            {
                var result = await importer.ImportAsync(path, ct);
                string target = Path.Join(_options.ProcessedPath, Path.GetFileName(path));
                File.Move(path, target, overwrite: true);
                _pending.TryRemove(path, out _);
                logger.LogInformation("Imported {File}: {Orders} orders, {Total:N2} TRY",
                    Path.GetFileName(path), result.Orders, result.Total);
            }
            catch (IOException ex) when (ex is not FileNotFoundException)
            {
                // The producer still holds the file; try again on a later tick.
                logger.LogDebug("{File} is busy: {Message}", Path.GetFileName(path), ex.Message);
                _pending[path] = time.GetUtcNow();
            }
        }
    }
}
