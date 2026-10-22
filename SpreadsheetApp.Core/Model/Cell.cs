using System.Numerics;
using SpreadsheetApp.Core.Nodes;
using SpreadsheetApp.Core.Parsing;

namespace SpreadsheetApp.Core.Model;

public class Cell
{
    public string Expression { get; internal set; } = string.Empty;
    public BigInteger? Value { get; internal set; }
    public ExpressionError? Error { get; internal set; }
    internal Node? Tree { get; set; }

    public bool HasError => Error != null;
}
