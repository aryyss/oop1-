using System.Numerics;
using SpreadsheetApp.Core.Model;

namespace SpreadsheetApp.Core.Nodes;

public class FunctionNode : Node
{
    public string Name { get; }
    public Node Argument { get; }

    public FunctionNode(string name, Node argument, int position) : base(position)
    {
        Name = name;
        Argument = argument;
    }

    public override BigInteger Evaluate(ICellProvider cells)
    {
        var value = Argument.Evaluate(cells);
        return Name switch
        {
            "inc" => value + BigInteger.One,
            "dec" => value - BigInteger.One,
            _ => throw new InvalidOperationException($"Невідома функція «{Name}»")
        };
    }

    public override IEnumerable<CellAddress> GetReferences() => Argument.GetReferences();
}
