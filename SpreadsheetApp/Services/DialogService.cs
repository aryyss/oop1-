using SpreadsheetApp.Core.ViewModels;
using SpreadsheetApp.Views;

namespace SpreadsheetApp.Services;

public class DialogService : IDialogService
{
    private const string Cancel = "Скасувати";

    private static Page CurrentPage =>
        Application.Current?.Windows.FirstOrDefault()?.Page
        ?? throw new InvalidOperationException("Вікно застосунку недоступне");

    public async Task<string?> ChooseAsync(string title, params string[] options)
    {
        var choice = await CurrentPage.DisplayActionSheet(title, Cancel, null, options);
        return choice == Cancel ? null : choice;
    }

    public Task<string?> PromptAsync(string title, string message, string? initialValue = null) =>
        CurrentPage.DisplayPromptAsync(title, message, "OK", Cancel, initialValue: initialValue ?? string.Empty);

    public Task<bool> ConfirmAsync(string title, string message) =>
        CurrentPage.DisplayAlert(title, message, "Так", "Ні");

    public Task ShowHelpAsync() => Shell.Current.GoToAsync(nameof(HelpPage));
}
