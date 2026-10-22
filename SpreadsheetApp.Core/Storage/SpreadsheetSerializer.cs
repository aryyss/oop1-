using System.Text.Json;
using SpreadsheetApp.Core.Model;
using SpreadsheetApp.Core.Parsing;

namespace SpreadsheetApp.Core.Storage;

public static class SpreadsheetSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static string Serialize(Spreadsheet sheet)
    {
        var data = new SheetData { Rows = sheet.RowCount, Columns = sheet.ColumnCount };

        foreach (var pair in sheet.Cells.OrderBy(p => p.Key.Row).ThenBy(p => p.Key.Column))
        {
            data.Cells[pair.Key.ToString()] = pair.Value.Expression;
        }

        return JsonSerializer.Serialize(data, Options);
    }

    public static Spreadsheet Deserialize(string json)
    {
        SheetData? data;
        try
        {
            data = JsonSerializer.Deserialize<SheetData>(json, Options);
        }
        catch (JsonException error)
        {
            throw new InvalidDataException("Файл має некоректний формат", error);
        }

        if (data == null)
        {
            throw new InvalidDataException("Файл порожній або має некоректний формат");
        }

        Spreadsheet sheet;
        try
        {
            sheet = new Spreadsheet(data.Rows, data.Columns);
        }
        catch (ArgumentOutOfRangeException error)
        {
            throw new InvalidDataException("У файлі вказано некоректний розмір таблиці", error);
        }

        foreach (var (key, text) in data.Cells ?? new Dictionary<string, string>())
        {
            if (!CellAddress.TryParse(key, out var address))
            {
                throw new InvalidDataException($"У файлі вказано некоректну адресу клітинки «{key}»");
            }

            try
            {
                sheet.SetExpression(address, text);
            }
            catch (ExpressionError error)
            {
                throw new InvalidDataException($"Клітинка {key}: {error.Message}", error);
            }
            catch (ArgumentOutOfRangeException error)
            {
                throw new InvalidDataException($"Клітинка {key} знаходиться поза межами таблиці", error);
            }
        }

        return sheet;
    }

    internal sealed class SheetData
    {
        public int Rows { get; set; }
        public int Columns { get; set; }
        public Dictionary<string, string> Cells { get; set; } = new();
    }
}
