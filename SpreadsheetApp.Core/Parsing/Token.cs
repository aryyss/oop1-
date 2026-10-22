namespace SpreadsheetApp.Core.Parsing;

public record Token(TokenType Type, string Text, int Position);
