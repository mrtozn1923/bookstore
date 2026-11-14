namespace Bookstore.ConcurrencyLab.LockFree;

public interface IConcurrentStack<T>
{
    void Push(T value);

    bool TryPop(out T value);
}

// Treiber (1986): a lock-free stack whose only shared word is the head reference
public sealed class TreiberStack<T> : IConcurrentStack<T>
{
    private sealed record Node(T Value, Node? Next);

    private Node? _head;

    public void Push(T value)
    {
        while (true)
        {
            Node? head = Volatile.Read(ref _head);
            Node node = new(value, head);
            if (Interlocked.CompareExchange(ref _head, node, head) == head)
                return;
        }
    }

    public bool TryPop(out T value)
    {
        while (true)
        {
            Node? head = Volatile.Read(ref _head);
            if (head is null)
            {
                value = default!;
                return false;
            }
            if (Interlocked.CompareExchange(ref _head, head.Next, head) == head)
            {
                value = head.Value;
                return true;
            }
        }
    }
}
