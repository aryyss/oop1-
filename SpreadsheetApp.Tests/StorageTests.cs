using SpreadsheetApp.Core.Model;
using SpreadsheetApp.Core.Storage;
using Xunit;

namespace SpreadsheetApp.Tests;

public class StorageTests
{
    private static Spreadsheet Sample()
    {
        var sheet = new Spreadsheet(5, 4);
        sheet.SetExpression(CellAddress.Parse("A1"), "4");
        sheet.SetExpression(CellAddress.Parse("B1"), "A1 ^ 2 + inc(A1) mod 3");
        sheet.SetExpression(CellAddress.Parse("D5"), "B1 * 2");
        return sheet;
    }

    private static void Save(ISpreadsheetStorage storage, Spreadsheet sheet, string name) =>
        storage.SaveAsync(sheet, name).GetAwaiter().GetResult();

    private static Spreadsheet Load(ISpreadsheetStorage storage, string name) =>
        storage.LoadAsync(name).GetAwaiter().GetResult();

    private static string Value(Spreadsheet sheet, string address) =>
        sheet.GetCell(CellAddress.Parse(address)).Value?.ToString() ?? "null";

    private static void WithTempDirectory(Action<string> action)
    {
        var directory = Path.Combine(Path.GetTempPath(), "spreadsheet-tests-" + Guid.NewGuid());
        try
        {
            action(directory);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    [Fact]
    public void Serializer_RoundTrip_KeepsSizeExpressionsAndValues()
    {
        var restored = SpreadsheetSerializer.Deserialize(SpreadsheetSerializer.Serialize(Sample()));

        Assert.Equal(5, restored.RowCount);
        Assert.Equal(4, restored.ColumnCount);
        Assert.Equal("A1 ^ 2 + inc(A1) mod 3", restored.GetCell(CellAddress.Parse("B1")).Expression);
        Assert.Equal("18", Value(restored, "B1"));
        Assert.Equal("36", Value(restored, "D5"));
    }

    [Fact]
    public void Serializer_EmptySheet_RoundTrip()
    {
        var restored = SpreadsheetSerializer.Deserialize(SpreadsheetSerializer.Serialize(new Spreadsheet(3, 2)));
        Assert.Equal(3, restored.RowCount);
        Assert.Equal(2, restored.ColumnCount);
        Assert.Equal(0, restored.Cells.Count);
    }

    [Fact]
    public void Serializer_RestoredSheetKeepsRecalculating()
    {
        var restored = SpreadsheetSerializer.Deserialize(SpreadsheetSerializer.Serialize(Sample()));
        restored.SetExpression(CellAddress.Parse("A1"), "1");
        Assert.Equal("6", Value(restored, "D5"));
    }

    [Fact]
    public void Serializer_RuntimeErrorsSurviveRoundTrip()
    {
        var sheet = new Spreadsheet(3, 3);
        sheet.SetExpression(CellAddress.Parse("A1"), "1 / 0");
        var restored = SpreadsheetSerializer.Deserialize(SpreadsheetSerializer.Serialize(sheet));
        Assert.True(restored.GetCell(CellAddress.Parse("A1")).HasError);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("{\"rows\":0,\"columns\":3,\"cells\":{}}")]
    [InlineData("{\"rows\":3,\"columns\":3,\"cells\":{\"1A\":\"1\"}}")]
    [InlineData("{\"rows\":3,\"columns\":3,\"cells\":{\"D1\":\"1\"}}")]
    [InlineData("{\"rows\":3,\"columns\":3,\"cells\":{\"A1\":\"1 +\"}}")]
    [InlineData("{\"rows\":3,\"columns\":3,\"cells\":{\"A1\":\"B1\",\"B1\":\"A1\"}}")]
    public void Deserialize_InvalidContent_Throws(string json)
    {
        Assert.Throws<InvalidDataException>(() => SpreadsheetSerializer.Deserialize(json));
    }

    [Fact]
    public void LocalStorage_SaveAndLoad()
    {
        WithTempDirectory(directory =>
        {
            var storage = new LocalFileStorage(directory);
            Save(storage, Sample(), "lab1");

            Assert.True(File.Exists(Path.Combine(directory, "lab1.json")));
            Assert.Equal("36", Value(Load(storage, "lab1"), "D5"));
        });
    }

    [Fact]
    public void LocalStorage_ExtensionInNameIsNotDuplicated()
    {
        WithTempDirectory(directory =>
        {
            var storage = new LocalFileStorage(directory);
            Save(storage, Sample(), "lab1.json");

            Assert.True(File.Exists(Path.Combine(directory, "lab1.json")));
            Assert.Equal("18", Value(Load(storage, "lab1.json"), "B1"));
        });
    }

    [Fact]
    public void LocalStorage_MissingFile_Throws()
    {
        WithTempDirectory(directory =>
        {
            Assert.Throws<FileNotFoundException>(() => Load(new LocalFileStorage(directory), "missing"));
        });
    }

    [Fact]
    public void LocalStorage_NameCannotEscapeDirectory()
    {
        WithTempDirectory(directory =>
        {
            Save(new LocalFileStorage(directory), Sample(), "../outside");
            Assert.True(File.Exists(Path.Combine(directory, "outside.json")));
        });
    }

    [Fact]
    public void LocalStorage_EmptyName_Throws()
    {
        WithTempDirectory(directory =>
        {
            Assert.Throws<ArgumentException>(() => Save(new LocalFileStorage(directory), Sample(), "  "));
        });
    }

    [Fact]
    public void LocalStorage_CorruptedFile_Throws()
    {
        WithTempDirectory(directory =>
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "bad.json"), "{ broken");
            Assert.Throws<InvalidDataException>(() => Load(new LocalFileStorage(directory), "bad"));
        });
    }
}
