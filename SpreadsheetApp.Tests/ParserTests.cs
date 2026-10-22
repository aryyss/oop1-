using SpreadsheetApp.Core.Model;
using SpreadsheetApp.Core.Parsing;
using Xunit;

namespace SpreadsheetApp.Tests;

public class ParserTests
{
    private static ExpressionError Fail(string text) =>
        Assert.Throws<ExpressionError>(() => new Parser().Parse(text));

    [Theory]
    [InlineData("1 + 2")]
    [InlineData("(1 + 2) * 3")]
    [InlineData("A1 ^ 2 + inc(A2) mod 3")]
    [InlineData("inc(dec(inc(7)))")]
    [InlineData("2 ^ 3 ^ 2")]
    public void Parse_ValidSyntax_DoesNotThrow(string text)
    {
        new Parser().Parse(text);
    }

    [Fact]
    public void Parse_EmptyText_ReportsErrorAtStart()
    {
        Assert.Equal(0, Fail("").Position);
    }

    [Theory]
    [InlineData("1 +", 3)]
    [InlineData("(1 + 2", 6)]
    [InlineData("1 2", 2)]
    [InlineData("inc 5", 4)]
    [InlineData("inc(5", 5)]
    [InlineData(")", 0)]
    [InlineData("* 3", 0)]
    [InlineData("1 + * 2", 4)]
    [InlineData("(1 + 2))", 7)]
    [InlineData("-5", 0)]
    [InlineData("1 + ", 4)]
    [InlineData("A1 B2", 3)]
    public void Parse_SyntaxError_ReportsPosition(string text, int position)
    {
        Assert.Equal(position, Fail(text).Position);
    }

    [Fact]
    public void GetReferences_CollectsAllCells()
    {
        var node = new Parser().Parse("A1 + B2 * inc(C3) - A1");
        var references = node.GetReferences().Select(a => a.ToString()).ToList();
        Assert.Equal("A1,B2,C3,A1", string.Join(",", references));
    }

    [Fact]
    public void GetReferences_NoCells_IsEmpty()
    {
        Assert.Empty(new Parser().Parse("1 + 2").GetReferences());
    }
}
