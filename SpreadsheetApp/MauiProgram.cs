using SpreadsheetApp.Core.Storage;
using SpreadsheetApp.Core.ViewModels;
using SpreadsheetApp.Services;
using SpreadsheetApp.Views;

namespace SpreadsheetApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        var dataDirectory = @"C:\SpreadsheetApp";

        builder.Services.AddSingleton(new GoogleAuthService(dataDirectory));
        builder.Services.AddSingleton<GoogleDriveStorage>();
        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton(provider => new MainViewModel(
            provider.GetRequiredService<IDialogService>(),
            new LocalFileStorage(dataDirectory),
            provider.GetRequiredService<GoogleDriveStorage>()));
        builder.Services.AddTransient<MainPage>();

        return builder.Build();
    }
}