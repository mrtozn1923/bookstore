using System.IO.Pipelines;

namespace Bookstore.IO.Pipelines;

// Reports how many bytes the consumer marks as consumed (only used by the backpressure demo).
public sealed class CountingPipeReader(PipeReader inner, Action<long> onConsumed) : PipeReader
{
    private ReadResult _last;

    public override async ValueTask<ReadResult> ReadAsync(CancellationToken ct = default) =>
        _last = await inner.ReadAsync(ct);

    public override bool TryRead(out ReadResult result)
    {
        bool ok = inner.TryRead(out result);
        if (ok) _last = result;
        return ok;
    }

    public override void AdvanceTo(SequencePosition consumed) => AdvanceTo(consumed, consumed);

    public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
    {
        onConsumed(_last.Buffer.Slice(0, consumed).Length);
        inner.AdvanceTo(consumed, examined);
    }

    public override void CancelPendingRead() => inner.CancelPendingRead();
    public override void Complete(Exception? exception = null) => inner.Complete(exception);
    public override ValueTask CompleteAsync(Exception? exception = null) => inner.CompleteAsync(exception);
}
