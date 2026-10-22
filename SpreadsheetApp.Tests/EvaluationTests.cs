using System.Numerics;
using SpreadsheetApp.Core.Parsing;
using Xunit;

namespace SpreadsheetApp.Tests;

public class EvaluationTests
{
    [Theory]
    [InlineData("1 + 2 * 3", "7")]
    [InlineData("(1 + 2) * 3", "9")]
    [InlineData("10 - 3 - 2", "5")]
    [InlineData("100 / 5 / 2", "10")]
    [InlineData("2 ^ 3 ^ 2", "512")]
    [InlineData("(2 ^ 3) ^ 2", "64")]
    [InlineData("2 * 3 ^ 2", "18")]
    [InlineData("7 div 2", "3")]
    [InlineData("7 mod 3", "1")]
    [InlineData("7 / 2", "3")]
    [InlineData("inc(5)", "6")]
    [InlineData("dec(5)", "4")]
    [InlineData("dec(inc(inc(5)))", "6")]
    [InlineData("inc(2 + 3) * 2", "12")]
    [InlineData("1 + 8 mod 3 * 2", "5")]
    [InlineData("0 ^ 0", "1")]
    [InlineData("5 ^ 0", "1")]
    [InlineData("  3   +   4  ", "7")]
    public void Evaluate_Expression(string text, string expected)
    {
        Assert.Equal(expected, Calc.Eval(text).ToString());
    }

    [Theory]
    [InlineData("(1 - 8) / 2", "-3")]
    [InlineData("(1 - 8) div 2", "-4")]
    [InlineData("(1 - 8) mod 3", "2")]
    [InlineData("7 mod (1 - 4)", "-2")]
    [InlineData("(1 - 8) div (1 - 3)", "3")]
    public void Evaluate_NegativeOperands(string text, string expected)
    {
        Assert.Equal(expected, Calc.Eval(text).ToString());
    }

    [Fact]
    public void Evaluate_ArbitraryLengthIntegers()
    {
        var result = Calc.Eval("100000000000000000000 * 100000000000000000000 + 1");
        Assert.Equal(BigInteger.Pow(10, 40) + 1, result);
    }

    [Fact]
    public void Evaluate_LargePower()
    {
        Assert.Equal(BigInteger.Pow(2, 1000), Calc.Eval("2 ^ 1000"));
    }

    [Fact]
    public void Evaluate_CellReferences()
    {
        var cells = new FakeCells().Set("A1", 4).Set("A2", 9);
        Assert.Equal(BigInteger.Parse("22"), Calc.Eval("A1 ^ 2 + inc(A2) mod 3 + 5", cells));
    }

    [Fact]
    public void Evaluate_MissingCell_PropagatesError()
    {
        Assert.Throws<ExpressionError>(() => Calc.Eval("A1 + 1"));
    }

    [Fact]
    public void Evaluate_MissingCell_ReportsReferencePosition()
    {
        var error = Assert.Throws<ExpressionError>(() => Calc.Eval("1 + A1"));
        Assert.Equal(4, error.Position);
    }

    [Theory]
    [InlineData("1 / 0", 2)]
    [InlineData("5 div 0", 2)]
    [InlineData("5 mod 0", 2)]
    [InlineData("1 + 5 / (3 - 3)", 6)]
    [InlineData("2 ^ (1 - 3)", 2)]
    [InlineData("2 ^ 20000000", 2)]
    [InlineData("10000000000000000000000 ^ 10000000", 24)]
    public void Evaluate_RuntimeError_ReportsOperatorPosition(string text, int position)
    {
        var error = Assert.Throws<ExpressionError>(() => Calc.Eval(text));
        Assert.Equal(position, error.Position);
    }
}
