namespace SpreadsheetApp.Core.Model;

public class DependencyGraph
{
    private readonly Dictionary<CellAddress, HashSet<CellAddress>> _references = new();
    private readonly Dictionary<CellAddress, HashSet<CellAddress>> _dependents = new();

    public void Update(CellAddress cell, IEnumerable<CellAddress> references)
    {
        if (_references.TryGetValue(cell, out var old))
        {
            foreach (var reference in old)
            {
                if (_dependents.TryGetValue(reference, out var set))
                {
                    set.Remove(cell);
                    if (set.Count == 0)
                    {
                        _dependents.Remove(reference);
                    }
                }
            }
            _references.Remove(cell);
        }

        var fresh = new HashSet<CellAddress>(references);
        if (fresh.Count == 0)
        {
            return;
        }

        _references[cell] = fresh;
        foreach (var reference in fresh)
        {
            if (!_dependents.TryGetValue(reference, out var set))
            {
                set = new HashSet<CellAddress>();
                _dependents[reference] = set;
            }
            set.Add(cell);
        }
    }

    public IReadOnlyCollection<CellAddress> GetReferences(CellAddress cell) =>
        _references.TryGetValue(cell, out var set) ? set.ToList() : new List<CellAddress>();

    public bool HasCycle(CellAddress cell)
    {
        var visited = new HashSet<CellAddress>();
        var stack = new Stack<CellAddress>();

        foreach (var reference in References(cell))
        {
            stack.Push(reference);
        }

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current == cell)
            {
                return true;
            }

            if (visited.Add(current))
            {
                foreach (var reference in References(current))
                {
                    stack.Push(reference);
                }
            }
        }

        return false;
    }

    public List<CellAddress> GetRecalcOrder(IEnumerable<CellAddress> roots)
    {
        var visited = new HashSet<CellAddress>();
        var order = new List<CellAddress>();

        foreach (var root in roots)
        {
            if (!visited.Add(root))
            {
                continue;
            }

            var stack = new Stack<(CellAddress Node, IEnumerator<CellAddress> Children)>();
            stack.Push((root, Dependents(root).GetEnumerator()));

            while (stack.Count > 0)
            {
                var (node, children) = stack.Peek();
                if (children.MoveNext())
                {
                    var next = children.Current;
                    if (visited.Add(next))
                    {
                        stack.Push((next, Dependents(next).GetEnumerator()));
                    }
                }
                else
                {
                    order.Add(node);
                    stack.Pop();
                }
            }
        }

        order.Reverse();
        return order;
    }

    private IEnumerable<CellAddress> References(CellAddress cell) =>
        _references.TryGetValue(cell, out var set) ? set : Enumerable.Empty<CellAddress>();

    private IEnumerable<CellAddress> Dependents(CellAddress cell) =>
        _dependents.TryGetValue(cell, out var set) ? set : Enumerable.Empty<CellAddress>();
}
