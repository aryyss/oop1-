using System.Globalization;
using System.Text;

namespace SpreadsheetApp.Core.Model;

public readonly record struct CellAddress(int Column, int Row)
{
    public override string ToString()
    {
        var letters = new StringBuilder();
        var n = Column + 1;
        while (n > 0)
        {
            n--;
            letters.Insert(0, (char)('A' + n % 26));
            n /= 26;
        }
        return letters.ToString() + (Row + 1).ToString(CultureInfo.InvariantCulture);
    }

    public static bool TryParse(string? text, out CellAddress address)
    {
        address = default;
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var i = 0;
        var column = 0;
        while (i < text.Length && char.IsAsciiLetter(text[i]))
        {
            column = column * 26 + (char.ToUpperInvariant(text[i]) - 'A' + 1);
            i++;
        }

        if (i == 0 || i > 3 || i == text.Length || text[i] == '0')
        {
            return false;
        }

        if (!int.TryParse(text.AsSpan(i), NumberStyles.None, CultureInfo.InvariantCulture, out var row) || row < 1)
        {
            return false;
        }

        address = new CellAddress(column - 1, row - 1);
        return true;
    }

    public static CellAddress Parse(string text)
    {
        if (TryParse(text, out var address))
        {
            return address;
        }

        throw new FormatException($"Некоректна адреса клітинки: «{text}»");
    }
}
