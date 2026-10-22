using System.Numerics;
using SpreadsheetApp.Core.Model;
using SpreadsheetApp.Core.Parsing;

namespace SpreadsheetApp.Tests;

public class FakeCells : ICellProvider
{
    private readonly Dictionary<CellAddress, BigInteger> _values = new();

    public FakeCells Set(string address, BigInteger value)
    {
        _values[CellAddress.Parse(address)] = value;
        return this;
    }

    public BigInteger GetValue(CellAddress address)
    {
        if (_values.TryGetValue(address, out var value))
        {
            return value;
        }
        throw new ExpressionError($"Клітинка {address} порожня", 0);
    }
}

public static class Calc
{
    public static BigInteger Eval(string text, ICellProvider? cells = null) =>
        new Parser().Parse(text).Evaluate(cells ?? new FakeCells());
}
