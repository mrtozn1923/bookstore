using System.Collections.Concurrent;

namespace Bookstore.ConcurrencyLab.LockFree;

// Same algorithm, but popped nodes go back to a pool and are reused: this brings the ABA problem back
public sealed class PooledTreiberStack : IConcurrentStack<int>
{
    private sealed class Node
    {
        public int Value;
        public Node? Next;
    }

    private readonly ConcurrentQueue<Node> _pool = new();
    private Node? _head;

    public void Push(int value)
    {
        Node node = _pool.TryDequeue(out Node? reused) ? reused : new Node();
        node.Value = value;
        while (true)
        {
            Node? head = Volatile.Read(ref _head);
            node.Next = head;
            if (Interlocked.CompareExchange(ref _head, node, head) == head)
                return;
        }
    }

    public bool TryPop(out int value)
    {
        while (true)
        {
            Node? head = Volatile.Read(ref _head);
            if (head is null)
            {
                value = 0;
                return false;
            }
            Node? next = head.Next;
            // head may have been popped, recycled and pushed again since we read it: CAS still succeeds
            if (Interlocked.CompareExchange(ref _head, next, head) == head)
            {
                value = head.Value;
                _pool.Enqueue(head);
                return true;
            }
        }
    }
}
