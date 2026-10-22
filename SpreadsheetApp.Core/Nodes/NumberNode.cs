using System.Numerics;
using SpreadsheetApp.Core.Model;

namespace SpreadsheetApp.Core.Nodes;

public class NumberNode : Node
{
    public BigInteger Value { get; }

    public NumberNode(BigInteger value, int position) : base(position)
    {
        Value = value;
    }

    public override BigInteger Evaluate(ICellProvider cells) => Value;

    public override IEnumerable<CellAddress> GetReferences() => Enumerable.Empty<CellAddress>();
}
