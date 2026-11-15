using System.Diagnostics.Metrics;
using Bookstore.ML.Model;

namespace Bookstore.ML.Api;

/// <summary>Prediction latency and out-of-range inputs as System.Diagnostics.Metrics instruments (OpenTelemetry can export them).</summary>
public sealed class PredictionMetrics
{
    public const string MeterName = "Bookstore.ML";

    private readonly Histogram<double> _duration;
    private readonly Counter<long> _outOfRange;

    public PredictionMetrics(IMeterFactory meterFactory)
    {
        Meter meter = meterFactory.Create(MeterName);
        _duration = meter.CreateHistogram<double>("bookstore.ml.prediction.duration", unit: "ms",
            description: "Time spent in PredictionEnginePool.Predict and the input checks");
        _outOfRange = meter.CreateCounter<long>("bookstore.ml.prediction.out_of_range",
            description: "Requests whose input is outside the range the model was trained on");
    }

    public void Record(TimeSpan elapsed, string modelVersion, bool outOfRange)
    {
        KeyValuePair<string, object?> version = new("model.version", modelVersion);
        _duration.Record(elapsed.TotalMilliseconds, version);
        if (outOfRange) _outOfRange.Add(1, version);
    }
}

/// <summary>The card of the deployed model, re-read when the file changes.</summary>
public sealed class ModelCardCache(string path)
{
    private readonly Lock _lock = new();
    private (DateTime WrittenUtc, ModelCard Card)? _cached;

    public ModelCard Current
    {
        get
        {
            DateTime written = File.GetLastWriteTimeUtc(path);
            lock (_lock)
            {
                if (_cached is not { } c || c.WrittenUtc != written)
                {
                    _cached = (written, ModelCard.Load(path));
                }
                return _cached.Value.Card;
            }
        }
    }
}
