using System.Threading.Channels;

namespace Bookstore.ConcurrencyLab.Models;

public interface IOrderDesk
{
    Task<bool> ReserveAsync(int quantity);
}

// Model 1: shared memory + lock. Every caller touches the stock; the lock makes check-and-act one step
public sealed class LockedOrderDesk(int stock) : IOrderDesk
{
    private readonly Lock _gate = new();
    private int _stock = stock;

    public Task<bool> ReserveAsync(int quantity)
    {
        lock (_gate)
        {
            if (_stock < quantity)
                return Task.FromResult(false);
            _stock -= quantity;
            return Task.FromResult(true);
        }
    }
}

// Model 2: actor. Only the actor's own loop touches the stock; callers send messages to its mailbox
public sealed class InventoryActor : IOrderDesk
{
    private abstract record Message;
    private sealed record ReserveMessage(int Quantity, TaskCompletionSource<bool> Reply) : Message;
    private sealed record GetStockMessage(TaskCompletionSource<int> Reply) : Message;
    private sealed record SetStockMessage(int Value) : Message;

    private readonly Channel<Message> _mailbox = Channel.CreateUnbounded<Message>(new UnboundedChannelOptions { SingleReader = true });
    private int _stock; // owned by the loop below, never shared

    public InventoryActor(int stock)
    {
        _stock = stock;
        _ = Task.Run(LoopAsync);
    }

    public Task<bool> ReserveAsync(int quantity) => Ask<bool>(reply => new ReserveMessage(quantity, reply));

    public Task<int> GetStockAsync() => Ask<int>(reply => new GetStockMessage(reply));

    public void SetStock(int value) => _mailbox.Writer.TryWrite(new SetStockMessage(value));

    private Task<T> Ask<T>(Func<TaskCompletionSource<T>, Message> create)
    {
        TaskCompletionSource<T> reply = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _mailbox.Writer.TryWrite(create(reply));
        return reply.Task;
    }

    private async Task LoopAsync()
    {
        // one message at a time: inside a handler nothing else can touch _stock
        await foreach (Message message in _mailbox.Reader.ReadAllAsync())
        {
            switch (message)
            {
                case ReserveMessage r when _stock >= r.Quantity:
                    _stock -= r.Quantity;
                    r.Reply.SetResult(true);
                    break;
                case ReserveMessage r:
                    r.Reply.SetResult(false);
                    break;
                case GetStockMessage g:
                    g.Reply.SetResult(_stock);
                    break;
                case SetStockMessage s:
                    _stock = s.Value;
                    break;
            }
        }
    }
}

// Model 3: CSP. Anonymous stages connected by channels; the reserve stage is the only owner of the stock
public sealed class OrderPipeline : IOrderDesk
{
    private sealed record OrderRequest(int Quantity, TaskCompletionSource<bool> Reply);

    private readonly Channel<OrderRequest> _incoming = Channel.CreateBounded<OrderRequest>(100);
    private readonly Channel<OrderRequest> _validated = Channel.CreateBounded<OrderRequest>(100);

    public OrderPipeline(int stock)
    {
        _ = Task.Run(ValidateAsync);
        _ = Task.Run(() => ReserveStageAsync(stock));
    }

    public async Task<bool> ReserveAsync(int quantity)
    {
        OrderRequest request = new(quantity, new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously));
        await _incoming.Writer.WriteAsync(request);
        return await request.Reply.Task;
    }

    private async Task ValidateAsync()
    {
        await foreach (OrderRequest request in _incoming.Reader.ReadAllAsync())
        {
            if (request.Quantity is < 1 or > 20)
                request.Reply.SetResult(false);
            else
                await _validated.Writer.WriteAsync(request);
        }
    }

    private async Task ReserveStageAsync(int stock)
    {
        await foreach (OrderRequest request in _validated.Reader.ReadAllAsync())
        {
            bool ok = stock >= request.Quantity;
            if (ok)
                stock -= request.Quantity;
            request.Reply.SetResult(ok);
        }
    }
}
