namespace SpreadsheetApp.Core.ViewModels;

public interface IDialogService
{
    Task<string?> ChooseAsync(string title, params string[] options);

    Task<string?> PromptAsync(string title, string message, string? initialValue = null);

    Task<bool> ConfirmAsync(string title, string message);

    Task ShowHelpAsync();
}
