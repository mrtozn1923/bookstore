namespace Bookstore.Institutional.Activities;

public enum NodeKind { Initial, Action, Decision, Merge, Fork, Join, ActivityFinal }

public sealed record Node(string Name, NodeKind Kind);

public sealed record Edge(Node From, Node To, Func<bool>? Guard = null);

// UML activity semantiğinin küçük bir modeli: token'lar edge'lerde bekler, node'lar kuralına göre ateşlenir
public sealed class TokenGame(IReadOnlyList<Edge> edges)
{
    private readonly Dictionary<Edge, int> tokens = edges.ToDictionary(e => e, _ => 0);

    public void Run(Node initial)
    {
        Offer(initial);
        bool fired = true;
        while (fired)
        {
            fired = false;
            foreach (Node node in edges.Select(e => e.To).Distinct())
            {
                List<Edge> incoming = edges.Where(e => e.To == node).ToList();
                // Action ve join: her gelen edge'de token olmalı (action için implicit join)
                bool ready = node.Kind is NodeKind.Action or NodeKind.Join or NodeKind.Fork or NodeKind.ActivityFinal
                    ? incoming.All(e => tokens[e] > 0)
                    : incoming.Any(e => tokens[e] > 0);
                if (!ready) continue;
                foreach (Edge e in node.Kind is NodeKind.Merge or NodeKind.Decision
                             ? [incoming.First(e => tokens[e] > 0)]
                             : incoming)
                    tokens[e]--;
                Console.WriteLine($"fire {node.Name}");
                if (node.Kind == NodeKind.ActivityFinal)
                {
                    Console.WriteLine("activity finished");
                    return;
                }
                Offer(node);
                fired = true;
            }
        }
        foreach ((Edge e, int count) in tokens.Where(t => t.Value > 0))
            Console.WriteLine($"stuck: {count} token(s) waiting on {e.From.Name} -> {e.To.Name}");
    }

    private void Offer(Node node)
    {
        List<Edge> outgoing = edges.Where(e => e.From == node).ToList();
        IEnumerable<Edge> targets = node.Kind == NodeKind.Decision
            ? [outgoing.First(e => e.Guard?.Invoke() ?? true)]
            : outgoing;
        foreach (Edge e in targets)
            tokens[e]++;
    }
}
