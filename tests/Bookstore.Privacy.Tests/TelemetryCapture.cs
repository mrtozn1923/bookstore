using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging.Testing;

namespace Bookstore.Privacy.Tests;

// Collects everything a code path writes to logs, traces and metrics during a test.
public sealed class TelemetryCapture : IDisposable
{
    private readonly ActivityListener _activities;
    private readonly MeterListener _meters = new();
    private readonly List<Activity> _stopped = [];
    private readonly List<KeyValuePair<string, object?>> _metricTags = [];

    public FakeLogCollector Logs { get; } = new();

    public TelemetryCapture(string sourceName)
    {
        _activities = new ActivityListener
        {
            ShouldListenTo = s => s.Name == sourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a => _stopped.Add(a),
        };
        ActivitySource.AddActivityListener(_activities);

        _meters.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == sourceName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _meters.SetMeasurementEventCallback<long>((_, _, tags, _) => _metricTags.AddRange(tags.ToArray()));
        _meters.Start();
    }

    // Every text that left the code path, labelled with where it went.
    public IEnumerable<string> Texts()
    {
        foreach (FakeLogRecord r in Logs.GetSnapshot())
        {
            yield return "log: " + r.Message;
            foreach (KeyValuePair<string, string?> kv in r.StructuredState ?? [])
            {
                yield return $"log field {kv.Key}: {kv.Value}";
            }
            if (r.Exception is not null)
            {
                yield return "log exception: " + r.Exception.Message;
            }
        }
        foreach (Activity a in _stopped)
        {
            yield return "span: " + a.DisplayName;
            foreach (KeyValuePair<string, object?> tag in a.TagObjects)
            {
                yield return $"span tag {tag.Key}: {tag.Value}";
            }
        }
    }

    public IEnumerable<string> FindLeaks(IEnumerable<string> personalValues) =>
        from text in Texts()
        from value in personalValues
        where text.Contains(value, StringComparison.OrdinalIgnoreCase)
        select $"{text}  <-- contains '{value}'";

    // Metric tags become time series; only low-cardinality, non-personal keys are allowed.
    public IEnumerable<string> UnexpectedMetricTags(IReadOnlySet<string> allowedKeys) =>
        _metricTags.Select(t => t.Key).Where(k => !allowedKeys.Contains(k)).Distinct();

    public void Dispose()
    {
        _activities.Dispose();
        _meters.Dispose();
    }
}
