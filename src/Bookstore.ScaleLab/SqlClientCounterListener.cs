using System.Collections.Concurrent;
using System.Diagnostics.Tracing;

namespace Bookstore.ScaleLab;

public sealed class SqlClientCounterListener : EventListener
{
    public ConcurrentDictionary<string, double> Latest { get; } = new();

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (eventSource.Name == "Microsoft.Data.SqlClient.EventSource")
        {
            EnableEvents(eventSource, EventLevel.Informational, EventKeywords.None,
                new Dictionary<string, string?> { ["EventCounterIntervalSec"] = "1" });
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        if (eventData.EventName != "EventCounters" || eventData.Payload is null)
        {
            return;
        }
        foreach (object? item in eventData.Payload)
        {
            if (item is IDictionary<string, object> counter && counter.TryGetValue("Name", out object? name))
            {
                object value = counter.TryGetValue("Mean", out object? mean) ? mean : counter["Increment"];
                Latest[(string)name] = Convert.ToDouble(value);
            }
        }
    }
}
