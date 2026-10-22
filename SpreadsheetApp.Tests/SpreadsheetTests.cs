using SpreadsheetApp.Core.Model;
using SpreadsheetApp.Core.Parsing;
using Xunit;

namespace SpreadsheetApp.Tests;

public class SpreadsheetTests
{
    private static void Set(Spreadsheet sheet, string address, string text) =>
        sheet.SetExpression(CellAddress.Parse(address), text);

    private static string Value(Spreadsheet sheet, string address) =>
        sheet.GetCell(CellAddress.Parse(address)).Value?.ToString() ?? "null";

    private static string Expression(Spreadsheet sheet, string address) =>
        sheet.GetCell(CellAddress.Parse(address)).Expression;

    private static Cell Cell(Spreadsheet sheet, string address) =>
        sheet.GetCell(CellAddress.Parse(address));

    [Fact]
    public void NewSheet_HasRequestedSize()
    {
        var sheet = new Spreadsheet(4, 6);
        Assert.Equal(4, sheet.RowCount);
        Assert.Equal(6, sheet.ColumnCount);
    }

    [Fact]
    public void SetExpression_ComputesValue()
    {
        var sheet = new Spreadsheet();
        Set(sheet, "A1", "2 + 3");
        Assert.Equal("5", Value(sheet, "A1"));
        Assert.Equal("2 + 3", Expression(sheet, "A1"));
    }

    [Fact]
    public void SetExpression_TrimsText()
    {
        var sheet = new Spreadsheet();
        Set(sheet, "A1", "   1 + 2  ");
        Assert.Equal("1 + 2", Expression(sheet, "A1"));
    }

    [Fact]
    public void Reference_RecalculatesWhenSourceChanges()
    {
        var sheet = new Spreadsheet();
        Set(sheet, "A1", "5");
        Set(sheet, "B1", "A1 * 2");
        Assert.Equal("10", Value(sheet, "B1"));

        Set(sheet, "A1", "7");
        Assert.Equal("14", Value(sheet, "B1"));
    }

    [Fact]
    public void Chain_RecalculatesAllDependents()
    {
        var sheet = new Spreadsheet();
        Set(sheet, "A1", "1");
        Set(sheet, "B1", "A1 + 1");
        Set(sheet, "C1", "B1 + 1");
        Set(sheet, "D1", "C1 + 1");
        Assert.Equal("4", Value(sheet, "D1"));

        Set(sheet, "A1", "10");
        Assert.Equal("13", Value(sheet, "D1"));
    }

    [Fact]
    public void Diamond_RecalculatesInCorrectOrder()
    {
        var sheet = new Spreadsheet();
        Set(sheet, "A1", "2");
        Set(sheet, "B1", "A1 + 1");
        Set(sheet, "C1", "A1 * 2");
        Set(sheet, "D1", "B1 + C1");
        Assert.Equal("7", Value(sheet, "D1"));

        Set(sheet, "A1", "3");
        Assert.Equal("10", Value(sheet, "D1"));
    }

    [Fact]
    public void FormulaDefinedBeforeItsSource_UpdatesLater()
    {
        var sheet = new Spreadsheet();
        Set(sheet, "A1", "B1 + 1");
        Assert.Equal("1", Value(sheet, "A1"));

        Set(sheet, "B1", "41");
        Assert.Equal("42", Value(sheet, "A1"));
    }

    [Fact]
    public void EmptyCellCountsAsZero()
    {
        var sheet = new Spreadsheet();
        Set(sheet, "B1", "A1 + 1");
        Assert.Equal("1", Value(sheet, "B1"));
    }

    [Fact]
    public void ClearingCell_RecalculatesDependents()
    {
        var sheet = new Spreadsheet();
        Set(sheet, "A1", "5");
        Set(sheet, "B1", "A1 + 1");
        Assert.Equal("6", Value(sheet, "B1"));

        Set(sheet, "A1", "");
        Assert.Equal("1", Value(sheet, "B1"));
        Assert.Equal("", Expression(sheet, "A1"));
        Assert.Equal("null", Value(sheet, "A1"));
    }

    [Fact]
    public void SyntaxError_IsRejectedAndCellKeepsOldContent()
    {
        var sheet = new Spreadsheet();
        Set(sheet, "A1", "5");

        var error = Assert.Throws<ExpressionError>(() => Set(sheet, "A1", "1 +"));

        Assert.Equal(3, error.Position);
        Assert.Equal("5", Expression(sheet, "A1"));
        Assert.Equal("5", Value(sheet, "A1"));
    }

    [Fact]
    public void SelfReference_IsRejected()
    {
        var sheet = new Spreadsheet();
        Assert.Throws<ExpressionError>(() => Set(sheet, "A1", "A1 + 1"));
        Assert.Equal("", Expression(sheet, "A1"));
    }

    [Fact]
    public void Cycle_IsRejectedAndGraphStaysIntact()
    {
        var sheet = new Spreadsheet();
        Set(sheet, "A1", "B1");
        Assert.Throws<ExpressionError>(() => Set(sheet, "B1", "A1"));
        Assert.Equal("", Expression(sheet, "B1"));

        Set(sheet, "B1", "7");
        Assert.Equal("7", Value(sheet, "A1"));
    }

    [Fact]
    public void LongCycle_IsRejected()
    {
        var sheet = new Spreadsheet();
        Set(sheet, "A1", "B1");
        Set(sheet, "B1", "C1");
        Assert.Throws<ExpressionError>(() => Set(sheet, "C1", "A1"));
    }

    [Fact]
    public void RejectedCycle_RestoresPreviousReferences()
    {
        var sheet = new Spreadsheet();
        Set(sheet, "A1", "B1");
        Set(sheet, "B1", "5");
        Assert.Throws<ExpressionError>(() => Set(sheet, "B1", "A1"));

        Assert.Equal("5", Expression(sheet, "B1"));
        Set(sheet, "B1", "6");
        Assert.Equal("6", Value(sheet, "A1"));
    }

    [Fact]
    public void RuntimeError_IsStoredInCell()
    {
        var sheet = new Spreadsheet();
        Set(sheet, "A1", "1 / 0");

        var cell = Cell(sheet, "A1");
        Assert.True(cell.HasError);
        Assert.Equal("null", Value(sheet, "A1"));
        Assert.Equal(2, cell.Error!.Position);
    }

    [Fact]
    public void RuntimeError_PropagatesToDependentsAndClears()
    {
        var sheet = new Spreadsheet();
        Set(sheet, "A1", "1 / 0");
        Set(sheet, "B1", "A1 + 1");

        Assert.True(Cell(sheet, "B1").HasError);
        Assert.Contains("A1", Cell(sheet, "B1").Error!.Message);
        Assert.Equal(0, Cell(sheet, "B1").Error!.Position);

        Set(sheet, "A1", "2");
        Assert.False(Cell(sheet, "B1").HasError);
        Assert.Equal("3", Value(sheet, "B1"));
    }

    [Fact]
    public void ReferenceOutsideSheet_IsRuntimeError()
    {
        var sheet = new Spreadsheet(3, 3);
        Set(sheet, "A1", "D1");
        Assert.True(Cell(sheet, "A1").HasError);
    }

    [Fact]
    public void AddColumn_FixesReferenceToNewCell()
    {
        var sheet = new Spreadsheet(3, 3);
        Set(sheet, "A1", "D1 + 1");
        Assert.True(Cell(sheet, "A1").HasError);

        sheet.AddColumn();

        Assert.Equal(4, sheet.ColumnCount);
        Assert.False(Cell(sheet, "A1").HasError);
        Assert.Equal("1", Value(sheet, "A1"));

        Set(sheet, "D1", "9");
        Assert.Equal("10", Value(sheet, "A1"));
    }

    [Fact]
    public void AddRow_IncreasesRowCount()
    {
        var sheet = new Spreadsheet(3, 3);
        sheet.AddRow();
        Assert.Equal(4, sheet.RowCount);
        Set(sheet, "A4", "1");
        Assert.Equal("1", Value(sheet, "A4"));
    }

    [Fact]
    public void RemoveLastColumn_BreaksReferencesToIt()
    {
        var sheet = new Spreadsheet(3, 3);
        Set(sheet, "C1", "5");
        Set(sheet, "A1", "C1 + 1");
        Assert.Equal("6", Value(sheet, "A1"));

        sheet.RemoveLastColumn();

        Assert.Equal(2, sheet.ColumnCount);
        Assert.True(Cell(sheet, "A1").HasError);
        Assert.Throws<ArgumentOutOfRangeException>(() => sheet.GetCell(CellAddress.Parse("C1")));
    }

    [Fact]
    public void RemoveLastRow_RemovesCellsOfThatRow()
    {
        var sheet = new Spreadsheet(3, 3);
        Set(sheet, "A3", "5");
        sheet.RemoveLastRow();

        Assert.Equal(2, sheet.RowCount);
        sheet.AddRow();
        Assert.Equal("", Expression(sheet, "A3"));
    }

    [Fact]
    public void RemoveLast_KeepsAtLeastOneRowAndColumn()
    {
        var sheet = new Spreadsheet(1, 1);
        Assert.Throws<InvalidOperationException>(() => sheet.RemoveLastRow());
        Assert.Throws<InvalidOperationException>(() => sheet.RemoveLastColumn());
    }

    [Fact]
    public void AddingBackAColumn_RestoresReferenceWithEmptyCell()
    {
        var sheet = new Spreadsheet(2, 2);
        Set(sheet, "B1", "5");
        Set(sheet, "A1", "B1");
        sheet.RemoveLastColumn();
        Assert.True(Cell(sheet, "A1").HasError);

        sheet.AddColumn();
        Assert.False(Cell(sheet, "A1").HasError);
        Assert.Equal("0", Value(sheet, "A1"));
    }

    [Fact]
    public void OutOfRangeAddress_Throws()
    {
        var sheet = new Spreadsheet(3, 3);
        Assert.Throws<ArgumentOutOfRangeException>(() => Set(sheet, "D1", "1"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Set(sheet, "A4", "1"));
    }

    [Fact]
    public void InvalidSize_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Spreadsheet(0, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Spreadsheet(5, 0));
    }

    [Fact]
    public void BigNumbersAcrossCells()
    {
        var sheet = new Spreadsheet();
        Set(sheet, "A1", "2 ^ 200");
        Set(sheet, "B1", "A1 * A1");
        Assert.Equal(System.Numerics.BigInteger.Pow(2, 400).ToString(), Value(sheet, "B1"));
    }

    [Fact]
    public void ExampleFromVariant5()
    {
        var sheet = new Spreadsheet();
        Set(sheet, "A1", "4");
        Set(sheet, "A2", "9");
        Set(sheet, "B2", "A1 ^ 2 + inc(A2) mod 3");
        Assert.Equal("17", Value(sheet, "B2"));
    }

    [Fact]
    public void LongChain_RecalculatesWithoutStackOverflow()
    {
        var sheet = new Spreadsheet(3000, 1);
        Set(sheet, "A1", "1");
        for (var row = 2; row <= 3000; row++)
        {
            Set(sheet, $"A{row}", $"A{row - 1} + 1");
        }
        Assert.Equal("3000", Value(sheet, "A3000"));

        Set(sheet, "A1", "101");
        Assert.Equal("3100", Value(sheet, "A3000"));
    }

    [Fact]
    public void Cells_ContainsOnlyNonEmptyCells()
    {
        var sheet = new Spreadsheet();
        Set(sheet, "A1", "1");
        Set(sheet, "B2", "2");
        Set(sheet, "A1", "");
        Assert.Equal(1, sheet.Cells.Count);
    }
}
