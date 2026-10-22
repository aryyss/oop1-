using System.Numerics;
using SpreadsheetApp.Core.Model;

namespace SpreadsheetApp.Core.Nodes;

public abstract class Node
{
    public int Position { get; }

    protected Node(int position)
    {
        Position = position;
    }

    public abstract BigInteger Evaluate(ICellProvider cells);

    public abstract IEnumerable<CellAddress> GetReferences();
}
