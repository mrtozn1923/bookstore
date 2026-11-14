namespace Bookstore.ConcurrencyLab.LockFree;

// No lock, no CAS: read head, then write head. Two threads can pop the same node or overwrite each other's push
public sealed class RacyStack : IConcurrentStack<int>
{
    private sealed record Node(int Value, Node? Next);

    private Node? _head;

    public void Push(int value) => Volatile.Write(ref _head, new Node(value, Volatile.Read(ref _head)));

    public bool TryPop(out int value)
    {
        Node? head = Volatile.Read(ref _head);
        if (head is null)
        {
            value = 0;
            return false;
        }
        Volatile.Write(ref _head, head.Next);
        value = head.Value;
        return true;
    }
}
