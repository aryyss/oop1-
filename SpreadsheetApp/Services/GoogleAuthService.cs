using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Util.Store;

namespace SpreadsheetApp.Services;

public class GoogleAuthService
{
    private const string CredentialsFile = "credentials.json";

    private readonly string _directory;
    private UserCredential? _credential;

    public GoogleAuthService(string directory)
    {
        _directory = directory;
    }

    public bool IsSignedIn => _credential != null;

    public async Task<UserCredential> SignInAsync(CancellationToken cancellationToken = default)
    {
        if (_credential != null)
        {
            return _credential;
        }

        var path = Path.Combine(_directory, CredentialsFile);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Не знайдено файл {CredentialsFile}. Покладіть його в папку {_directory}.", path);
        }

        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
        var secrets = GoogleClientSecrets.FromStream(stream).Secrets;

        _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            secrets,
            new[] { DriveService.Scope.DriveFile },
            "user",
            cancellationToken,
            new FileDataStore(Path.Combine(_directory, "google-token"), true));

        return _credential;
    }
}
