namespace SpreadsheetApp.Core.Parsing;

public class ExpressionError : Exception
{
    public int Position { get; }

    public ExpressionError(string message, int position) : base(message)
    {
        Position = position;
    }
}
