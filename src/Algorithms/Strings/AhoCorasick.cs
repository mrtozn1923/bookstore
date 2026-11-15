namespace Algorithms.Strings;

// A trie of all patterns plus failure links: one pass over the text finds every occurrence of every pattern
public sealed class AhoCorasick
{
    private readonly List<Dictionary<char, int>> _next = [[]];
    private readonly List<int> _fail = [0];
    private readonly List<List<int>> _output = [[]]; // pattern indexes that end at this node
    private readonly string[] _patterns;

    public AhoCorasick(IEnumerable<string> patterns)
    {
        _patterns = [.. patterns.Where(p => p.Length > 0)];
        for (int i = 0; i < _patterns.Length; i++) AddToTrie(_patterns[i], i);
        BuildFailureLinks();
    }

    public int NodeCount => _next.Count;

    public IEnumerable<(int Index, string Pattern)> FindAll(string text)
    {
        int state = 0;
        for (int i = 0; i < text.Length; i++)
        {
            state = Step(state, text[i]);
            foreach (int p in _output[state])
                yield return (i - _patterns[p].Length + 1, _patterns[p]);
        }
    }

    public bool ContainsAny(string text)
    {
        int state = 0;
        foreach (char c in text)
        {
            state = Step(state, c);
            if (_output[state].Count > 0) return true;
        }

        return false;
    }

    private int Step(int state, char c)
    {
        int next;
        while (!_next[state].TryGetValue(c, out next))
        {
            if (state == 0) return 0;
            state = _fail[state]; // longest proper suffix that is also a trie path
        }

        return next;
    }

    private void AddToTrie(string pattern, int index)
    {
        int node = 0;
        foreach (char c in pattern)
        {
            if (!_next[node].TryGetValue(c, out int child))
            {
                child = _next.Count;
                _next.Add([]);
                _fail.Add(0);
                _output.Add([]);
                _next[node][c] = child;
            }

            node = child;
        }

        _output[node].Add(index);
    }

    // Breadth-first: a node's failure link always points to a shallower node, which is already finished
    private void BuildFailureLinks()
    {
        Queue<int> queue = new();
        foreach (int child in _next[0].Values) queue.Enqueue(child); // depth 1 fails to the root
        while (queue.Count > 0)
        {
            int node = queue.Dequeue();
            foreach ((char c, int child) in _next[node])
            {
                int f = _fail[node];
                while (f != 0 && !_next[f].ContainsKey(c)) f = _fail[f];
                _fail[child] = _next[f].TryGetValue(c, out int target) && target != child ? target : 0;
                _output[child].AddRange(_output[_fail[child]]); // "she" also reports "he"
                queue.Enqueue(child);
            }
        }
    }
}
