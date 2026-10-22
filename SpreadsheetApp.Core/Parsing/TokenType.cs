namespace SpreadsheetApp.Core.Parsing;

public enum TokenType
{
    Number,
    CellRef,
    Operator,
    Function,
    LParen,
    RParen,
    End
}
