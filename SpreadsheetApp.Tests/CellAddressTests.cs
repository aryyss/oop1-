using SpreadsheetApp.Core.Model;
using Xunit;

namespace SpreadsheetApp.Tests;

public class CellAddressTests
{
    [Theory]
    [InlineData(0, 0, "A1")]
    [InlineData(25, 0, "Z1")]
    [InlineData(26, 0, "AA1")]
    [InlineData(27, 11, "AB12")]
    [InlineData(51, 99, "AZ100")]
    [InlineData(52, 0, "BA1")]
    public void ToString_ReturnsLettersAndRow(int column, int row, string expected)
    {
        Assert.Equal(expected, new CellAddress(column, row).ToString());
    }

    [Theory]
    [InlineData("A1", 0, 0)]
    [InlineData("a1", 0, 0)]
    [InlineData("Z9", 25, 8)]
    [InlineData("AA1", 26, 0)]
    [InlineData("ab12", 27, 11)]
    public void TryParse_ValidAddress(string text, int column, int row)
    {
        Assert.True(CellAddress.TryParse(text, out var address));
        Assert.Equal(new CellAddress(column, row), address);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("1")]
    [InlineData("A0")]
    [InlineData("A01")]
    [InlineData("AAAA1")]
    [InlineData("A1B")]
    [InlineData("A-1")]
    public void TryParse_InvalidAddress(string text)
    {
        Assert.False(CellAddress.TryParse(text, out _));
    }

    [Fact]
    public void Parse_InvalidAddress_Throws()
    {
        Assert.Throws<FormatException>(() => CellAddress.Parse("12"));
    }

    [Fact]
    public void RoundTrip_ParseAndToString()
    {
        for (var column = 0; column < 800; column += 7)
        {
            var original = new CellAddress(column, column % 50);
            Assert.Equal(original, CellAddress.Parse(original.ToString()));
        }
    }
}
