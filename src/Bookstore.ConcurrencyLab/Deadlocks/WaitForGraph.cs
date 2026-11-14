namespace Bookstore.ConcurrencyLab.Deadlocks;

// Edge "A -> B" means: thread A waits for a lock that thread B holds
public sealed class WaitForGraph
{
    private readonly Dictionary<string, List<string>> _edges = [];

    public void AddWait(string waiter, string holder)
    {
        if (!_edges.TryGetValue(waiter, out List<string>? targets))
            _edges[waiter] = targets = [];
        targets.Add(holder);
    }

    // Depth-first search; returns the first cycle found, or an empty list
    public IReadOnlyList<string> FindCycle()
    {
        HashSet<string> done = [];
        List<string> path = [];
        foreach (string start in _edges.Keys)
        {
            List<string> cycle = Visit(start, path, done);
            if (cycle.Count > 0)
                return cycle;
        }
        return [];
    }

    private List<string> Visit(string node, List<string> path, HashSet<string> done)
    {
        int index = path.IndexOf(node);
        if (index >= 0)
            return [.. path[index..], node];
        if (!done.Add(node))
            return [];
        path.Add(node);
        foreach (string next in _edges.GetValueOrDefault(node) ?? [])
        {
            List<string> cycle = Visit(next, path, done);
            if (cycle.Count > 0)
                return cycle;
        }
        path.RemoveAt(path.Count - 1);
        return [];
    }
}
