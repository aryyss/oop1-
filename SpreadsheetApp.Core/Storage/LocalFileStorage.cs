using SpreadsheetApp.Core.Model;

namespace SpreadsheetApp.Core.Storage;

public class LocalFileStorage : ISpreadsheetStorage
{
    private const string Extension = ".json";

    private readonly string _directory;

    public LocalFileStorage(string directory)
    {
        _directory = directory;
    }

    public async Task SaveAsync(Spreadsheet sheet, string name, CancellationToken cancellationToken = default)
    {
        var path = PathFor(name);
        Directory.CreateDirectory(_directory);
        await File.WriteAllTextAsync(path, SpreadsheetSerializer.Serialize(sheet), cancellationToken);
    }

    public async Task<Spreadsheet> LoadAsync(string name, CancellationToken cancellationToken = default)
    {
        var path = PathFor(name);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Файл «{name}» не знайдено", path);
        }

        return SpreadsheetSerializer.Deserialize(await File.ReadAllTextAsync(path, cancellationToken));
    }

    private string PathFor(string name)
    {
        var safeName = Path.GetFileNameWithoutExtension(Path.GetFileName(name ?? string.Empty));
        if (string.IsNullOrWhiteSpace(safeName))
        {
            throw new ArgumentException("Назва файлу не може бути порожньою", nameof(name));
        }

        return Path.Combine(_directory, safeName + Extension);
    }
}
