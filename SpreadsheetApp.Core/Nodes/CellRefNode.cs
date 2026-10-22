using System.Numerics;
using SpreadsheetApp.Core.Model;
using SpreadsheetApp.Core.Parsing;

namespace SpreadsheetApp.Core.Nodes;

public class CellRefNode : Node
{
    public CellAddress Address { get; }

    public CellRefNode(CellAddress address, int position) : base(position)
    {
        Address = address;
    }

    public override BigInteger Evaluate(ICellProvider cells)
    {
        try
        {
            return cells.GetValue(Address);
        }
        catch (ExpressionError error)
        {
            throw new ExpressionError(error.Message, Position);
        }
    }

    public override IEnumerable<CellAddress> GetReferences()
    {
        yield return Address;
    }
}
