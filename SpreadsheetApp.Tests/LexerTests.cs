using SpreadsheetApp.Core.Parsing;
using Xunit;

namespace SpreadsheetApp.Tests;

public class LexerTests
{
    private static string Describe(string text) =>
        string.Join(" ", new Lexer().Tokenize(text).Select(t => $"{t.Type}:{t.Text}@{t.Position}"));

    [Fact]
    public void Tokenize_CellsNumbersAndOperators()
    {
        Assert.Equal("CellRef:A1@0 Operator:+@3 Number:12@5 End:@7", Describe("A1 + 12"));
    }

    [Fact]
    public void Tokenize_KeywordsAreCaseInsensitive()
    {
        Assert.Equal(
            "Number:5@0 Operator:mod@2 Number:3@6 Operator:div@8 Function:inc@12 LParen:(@15 Number:1@16 RParen:)@17 End:@18",
            Describe("5 MOD 3 Div INC(1)"));
    }

    [Fact]
    public void Tokenize_CellReferenceIsUpperCased()
    {
        Assert.Equal("CellRef:AB12@0 End:@4", Describe("ab12"));
    }

    [Fact]
    public void Tokenize_LongNumberKeepsAllDigits()
    {
        var tokens = new Lexer().Tokenize("123456789012345678901234567890");
        Assert.Equal("123456789012345678901234567890", tokens[0].Text);
    }

    [Fact]
    public void Tokenize_EmptyText_OnlyEnd()
    {
        Assert.Equal("End:@0", Describe(""));
    }

    [Fact]
    public void Tokenize_InvalidCharacter_ReportsPosition()
    {
        var error = Assert.Throws<ExpressionError>(() => new Lexer().Tokenize("2 # 3"));
        Assert.Equal(2, error.Position);
    }

    [Fact]
    public void Tokenize_UnknownIdentifier_ReportsPosition()
    {
        var error = Assert.Throws<ExpressionError>(() => new Lexer().Tokenize("1 + foo"));
        Assert.Equal(4, error.Position);
    }
}
