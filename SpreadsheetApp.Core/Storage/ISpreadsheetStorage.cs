using SpreadsheetApp.Core.Model;

namespace SpreadsheetApp.Core.Storage;

public interface ISpreadsheetStorage
{
    Task SaveAsync(Spreadsheet sheet, string name, CancellationToken cancellationToken = default);

    Task<Spreadsheet> LoadAsync(string name, CancellationToken cancellationToken = default);
}
