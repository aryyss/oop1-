using System.ComponentModel;
using System.Runtime.CompilerServices;
using SpreadsheetApp.Core.Model;
using SpreadsheetApp.Core.Parsing;
using SpreadsheetApp.Core.Storage;

namespace SpreadsheetApp.Core.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    public const string LocalOption = "На цьому комп'ютері";
    public const string DriveOption = "Google Drive";

    private const int DefaultRows = 10;
    private const int DefaultColumns = 10;

    private readonly IDialogService _dialogs;
    private readonly ISpreadsheetStorage _localStorage;
    private readonly ISpreadsheetStorage _driveStorage;

    private Spreadsheet _sheet = new(DefaultRows, DefaultColumns);
    private DisplayMode _mode = DisplayMode.Value;
    private string _statusMessage = "Готово до роботи";
    private string? _fileName;
    private bool _isDirty;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? TableChanged;
    public event EventHandler? ValuesChanged;

    public MainViewModel(IDialogService dialogs, ISpreadsheetStorage localStorage, ISpreadsheetStorage driveStorage)
    {
        _dialogs = dialogs;
        _localStorage = localStorage;
        _driveStorage = driveStorage;

        NewCommand = new AsyncRelayCommand(NewAsync);
        OpenCommand = new AsyncRelayCommand(OpenAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        ShowHelpCommand = new AsyncRelayCommand(() => _dialogs.ShowHelpAsync());
        AddRowCommand = new RelayCommand(AddRow);
        RemoveRowCommand = new RelayCommand(RemoveRow);
        AddColumnCommand = new RelayCommand(AddColumn);
        RemoveColumnCommand = new RelayCommand(RemoveColumn);
        ToggleModeCommand = new RelayCommand(ToggleMode);
    }

    public Spreadsheet Sheet => _sheet;

    public DisplayMode Mode => _mode;

    public string ModeTitle => _mode == DisplayMode.Expression ? "Режим: ВИРАЗ" : "Режим: ЗНАЧЕННЯ";

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public string? FileName => _fileName;

    public bool IsDirty => _isDirty;

    public AsyncRelayCommand NewCommand { get; }
    public AsyncRelayCommand OpenCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }
    public AsyncRelayCommand ShowHelpCommand { get; }
    public RelayCommand AddRowCommand { get; }
    public RelayCommand RemoveRowCommand { get; }
    public RelayCommand AddColumnCommand { get; }
    public RelayCommand RemoveColumnCommand { get; }
    public RelayCommand ToggleModeCommand { get; }

    public string GetDisplayText(CellAddress address)
    {
        var cell = _sheet.GetCell(address);

        if (_mode == DisplayMode.Expression)
        {
            return cell.Expression;
        }

        if (cell.HasError)
        {
            return "#ПОМИЛКА";
        }

        return cell.Value?.ToString() ?? string.Empty;
    }

    public string GetEditText(CellAddress address) => _sheet.GetCell(address).Expression;

    public bool IsErrorShown(CellAddress address) =>
        _mode == DisplayMode.Value && _sheet.GetCell(address).HasError;

    public void Select(CellAddress address)
    {
        var cell = _sheet.GetCell(address);

        if (cell.HasError)
        {
            StatusMessage = $"Клітинка {address}: {cell.Error!.Message}";
        }
        else if (cell.Expression.Length == 0)
        {
            StatusMessage = $"Клітинка {address}: порожня";
        }
        else
        {
            StatusMessage = $"Клітинка {address}: {cell.Expression}";
        }
    }

    public bool CommitCell(CellAddress address, string? text)
    {
        text = (text ?? string.Empty).Trim();

        if (text == _sheet.GetCell(address).Expression)
        {
            ValuesChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        try
        {
            _sheet.SetExpression(address, text);
        }
        catch (ExpressionError error)
        {
            StatusMessage = $"Помилка в клітинці {address} (символ {error.Position + 1}): {error.Message}";
            ValuesChanged?.Invoke(this, EventArgs.Empty);
            return false;
        }

        _isDirty = true;
        Select(address);
        ValuesChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private void AddRow() => ChangeStructure(_sheet.AddRow, "Додано рядок");

    private void RemoveRow() => ChangeStructure(_sheet.RemoveLastRow, "Видалено останній рядок");

    private void AddColumn() => ChangeStructure(_sheet.AddColumn, "Додано стовпчик");

    private void RemoveColumn() => ChangeStructure(_sheet.RemoveLastColumn, "Видалено останній стовпчик");

    private void ChangeStructure(Action change, string message)
    {
        try
        {
            change();
        }
        catch (InvalidOperationException error)
        {
            StatusMessage = error.Message;
            return;
        }

        _isDirty = true;
        StatusMessage = $"{message}. Розмір таблиці: {_sheet.RowCount} × {_sheet.ColumnCount}";
        TableChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ToggleMode()
    {
        _mode = _mode == DisplayMode.Expression ? DisplayMode.Value : DisplayMode.Expression;
        OnPropertyChanged(nameof(Mode));
        OnPropertyChanged(nameof(ModeTitle));
        ValuesChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task NewAsync()
    {
        if (!await ConfirmDiscardAsync("Нова таблиця"))
        {
            return;
        }

        ReplaceSheet(new Spreadsheet(DefaultRows, DefaultColumns), null);
        StatusMessage = "Створено нову таблицю";
    }

    private async Task SaveAsync()
    {
        var storage = await ChooseStorageAsync("Зберегти таблицю");
        if (storage == null)
        {
            return;
        }

        var name = await _dialogs.PromptAsync("Зберегти таблицю", "Назва файлу:", _fileName ?? "table");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        name = name.Trim();
        try
        {
            StatusMessage = "Збереження...";
            await storage.SaveAsync(_sheet, name);
            _fileName = name;
            _isDirty = false;
            OnPropertyChanged(nameof(FileName));
            StatusMessage = $"Таблицю збережено: {name}";
        }
        catch (Exception error)
        {
            StatusMessage = $"Не вдалося зберегти: {error.Message}";
        }
    }

    private async Task OpenAsync()
    {
        if (!await ConfirmDiscardAsync("Відкрити таблицю"))
        {
            return;
        }

        var storage = await ChooseStorageAsync("Відкрити таблицю");
        if (storage == null)
        {
            return;
        }

        var name = await _dialogs.PromptAsync("Відкрити таблицю", "Назва файлу:");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        name = name.Trim();
        try
        {
            StatusMessage = "Завантаження...";
            var sheet = await storage.LoadAsync(name);
            ReplaceSheet(sheet, name);
            StatusMessage = $"Відкрито таблицю: {name}";
        }
        catch (Exception error)
        {
            StatusMessage = $"Не вдалося відкрити: {error.Message}";
        }
    }

    private async Task<bool> ConfirmDiscardAsync(string title)
    {
        if (!_isDirty)
        {
            return true;
        }

        return await _dialogs.ConfirmAsync(title, "Незбережені зміни буде втрачено. Продовжити?");
    }

    private async Task<ISpreadsheetStorage?> ChooseStorageAsync(string title)
    {
        var choice = await _dialogs.ChooseAsync(title, LocalOption, DriveOption);
        return choice switch
        {
            LocalOption => _localStorage,
            DriveOption => _driveStorage,
            _ => null
        };
    }

    private void ReplaceSheet(Spreadsheet sheet, string? fileName)
    {
        _sheet = sheet;
        _fileName = fileName;
        _isDirty = false;
        OnPropertyChanged(nameof(Sheet));
        OnPropertyChanged(nameof(FileName));
        TableChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(name);
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
