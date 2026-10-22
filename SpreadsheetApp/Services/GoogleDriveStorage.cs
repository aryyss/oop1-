using System.Text;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using SpreadsheetApp.Core.Model;
using SpreadsheetApp.Core.Storage;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace SpreadsheetApp.Services;

public class GoogleDriveStorage : ISpreadsheetStorage
{
    private const string MimeType = "application/json";

    private readonly GoogleAuthService _auth;

    public GoogleDriveStorage(GoogleAuthService auth)
    {
        _auth = auth;
    }

    public async Task SaveAsync(Spreadsheet sheet, string name, CancellationToken cancellationToken = default)
    {
        using var service = await CreateServiceAsync(cancellationToken);
        var fileName = NormalizeName(name);
        var bytes = Encoding.UTF8.GetBytes(SpreadsheetSerializer.Serialize(sheet));
        using var content = new MemoryStream(bytes);

        var existingId = await FindFileIdAsync(service, fileName, cancellationToken);

        IUploadProgress progress;
        if (existingId == null)
        {
            var request = service.Files.Create(new DriveFile { Name = fileName, MimeType = MimeType }, content, MimeType);
            progress = await request.UploadAsync(cancellationToken);
        }
        else
        {
            var request = service.Files.Update(new DriveFile(), existingId, content, MimeType);
            progress = await request.UploadAsync(cancellationToken);
        }

        if (progress.Status != UploadStatus.Completed)
        {
            throw progress.Exception ?? new IOException("Не вдалося завантажити файл на Google Drive");
        }
    }

    public async Task<Spreadsheet> LoadAsync(string name, CancellationToken cancellationToken = default)
    {
        using var service = await CreateServiceAsync(cancellationToken);
        var fileName = NormalizeName(name);

        var id = await FindFileIdAsync(service, fileName, cancellationToken)
            ?? throw new FileNotFoundException($"Файл «{fileName}» не знайдено на Google Drive");

        using var content = new MemoryStream();
        var progress = await service.Files.Get(id).DownloadAsync(content, cancellationToken);
        if (progress.Status != DownloadStatus.Completed)
        {
            throw progress.Exception ?? new IOException("Не вдалося завантажити файл з Google Drive");
        }

        return SpreadsheetSerializer.Deserialize(Encoding.UTF8.GetString(content.ToArray()));
    }

    private async Task<DriveService> CreateServiceAsync(CancellationToken cancellationToken)
    {
        var credential = await _auth.SignInAsync(cancellationToken);
        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "SpreadsheetApp"
        });
    }

    private static async Task<string?> FindFileIdAsync(DriveService service, string fileName, CancellationToken cancellationToken)
    {
        var escaped = fileName.Replace("\\", "\\\\").Replace("'", "\\'");
        var request = service.Files.List();
        request.Q = $"name = '{escaped}' and trashed = false";
        request.Fields = "files(id, name)";

        var response = await request.ExecuteAsync(cancellationToken);
        return response.Files?.FirstOrDefault()?.Id;
    }

    private static string NormalizeName(string name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("Назва файлу не може бути порожньою", nameof(name));
        }

        return trimmed.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? trimmed : trimmed + ".json";
    }
}
