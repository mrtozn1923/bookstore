using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Bookstore.Api.Diagnostics;

// Listens to EF Core's DiagnosticListener and logs commands slower than the threshold.
public sealed class SlowCommandObserver(ILogger<SlowCommandObserver> logger, TimeSpan threshold)
    : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>
{
    public void OnNext(DiagnosticListener listener)
    {
        if (listener.Name == DbLoggerCategory.Name)
        {
            listener.Subscribe(this);
        }
    }

    public void OnNext(KeyValuePair<string, object?> e)
    {
        if (e.Key == RelationalEventId.CommandExecuted.Name
            && e.Value is CommandExecutedEventData data
            && data.Duration > threshold)
        {
            logger.LogWarning("Slow command ({ElapsedMs} ms): {CommandText}", (long)data.Duration.TotalMilliseconds, data.Command.CommandText);
        }
    }

    public void OnCompleted() { }
    public void OnError(Exception error) { }
}
