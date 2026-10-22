using SpreadsheetApp.Core.Model;
using SpreadsheetApp.Core.Storage;
using SpreadsheetApp.Core.ViewModels;
using Xunit;

namespace SpreadsheetApp.Tests;

public class FakeDialogs : IDialogService
{
    public Queue<string?> Choices { get; } = new();
    public Queue<string?> Prompts { get; } = new();
    public bool ConfirmAnswer { get; set; } = true;
    public int ConfirmCalls { get; private set; }
    public int HelpCalls { get; private set; }

    public Task<string?> ChooseAsync(string title, params string[] options) =>
        Task.FromResult(Choices.Count > 0 ? Choices.Dequeue() : null);

    public Task<string?> PromptAsync(string title, string message, string? initialValue = null) =>
        Task.FromResult(Prompts.Count > 0 ? Prompts.Dequeue() : null);

    public Task<bool> ConfirmAsync(string title, string message)
    {
        ConfirmCalls++;
        return Task.FromResult(ConfirmAnswer);
    }

    public Task ShowHelpAsync()
    {
        HelpCalls++;
        return Task.CompletedTask;
    }
}

public class MemoryStorage : ISpreadsheetStorage
{
    public Dictionary<string, string> Files { get; } = new();
    public Exception? FailWith { get; set; }

    public Task SaveAsync(Spreadsheet sheet, string name, CancellationToken cancellationToken = default)
    {
        if (FailWith != null)
        {
            throw FailWith;
        }

        Files[name] = SpreadsheetSerializer.Serialize(sheet);
        return Task.CompletedTask;
    }

    public Task<Spreadsheet> LoadAsync(string name, CancellationToken cancellationToken = default)
    {
        if (!Files.TryGetValue(name, out var json))
        {
            throw new FileNotFoundException($"Файл «{name}» не знайдено");
        }

        return Task.FromResult(SpreadsheetSerializer.Deserialize(json));
    }
}

public class MainViewModelTests
{
    private readonly FakeDialogs _dialogs = new();
    private readonly MemoryStorage _local = new();
    private readonly MemoryStorage _drive = new();
    private readonly MainViewModel _vm;

    public MainViewModelTests()
    {
        _vm = new MainViewModel(_dialogs, _local, _drive);
    }

    private static CellAddress A(string text) => CellAddress.Parse(text);

    [Fact]
    public void CommitCell_ValidExpression_ShowsValueAndRaisesEvent()
    {
        var raised = 0;
        _vm.ValuesChanged += (_, _) => raised++;

        Assert.True(_vm.CommitCell(A("A1"), "2 + 3"));

        Assert.Equal("5", _vm.GetDisplayText(A("A1")));
        Assert.True(_vm.IsDirty);
        Assert.True(raised > 0);
    }

    [Fact]
    public void ToggleMode_SwitchesBetweenExpressionAndValue()
    {
        _vm.CommitCell(A("A1"), "2 + 3");
        Assert.Equal("Режим: ЗНАЧЕННЯ", _vm.ModeTitle);

        _vm.ToggleModeCommand.Execute(null);
        Assert.Equal(DisplayMode.Expression, _vm.Mode);
        Assert.Equal("Режим: ВИРАЗ", _vm.ModeTitle);
        Assert.Equal("2 + 3", _vm.GetDisplayText(A("A1")));

        _vm.ToggleModeCommand.Execute(null);
        Assert.Equal("5", _vm.GetDisplayText(A("A1")));
    }

    [Fact]
    public void CommitCell_SyntaxError_ReportsPositionAndKeepsCell()
    {
        _vm.CommitCell(A("A1"), "7");

        Assert.False(_vm.CommitCell(A("A1"), "1 +"));

        Assert.Contains("символ 4", _vm.StatusMessage);
        Assert.Equal("7", _vm.GetDisplayText(A("A1")));
    }

    [Fact]
    public void CommitCell_RuntimeError_IsShownAsErrorAndExplainedOnSelect()
    {
        _vm.CommitCell(A("A1"), "1 / 0");

        Assert.Equal("#ПОМИЛКА", _vm.GetDisplayText(A("A1")));
        Assert.True(_vm.IsErrorShown(A("A1")));

        _vm.Select(A("A1"));
        Assert.Contains("Ділення на нуль", _vm.StatusMessage);

        _vm.ToggleModeCommand.Execute(null);
        Assert.False(_vm.IsErrorShown(A("A1")));
        Assert.Equal("1 / 0", _vm.GetDisplayText(A("A1")));
    }

    [Fact]
    public void CommitCell_SameText_DoesNotMarkDirty()
    {
        Assert.True(_vm.CommitCell(A("A1"), ""));
        Assert.False(_vm.IsDirty);
    }

    [Fact]
    public void Select_ShowsExpressionOfCell()
    {
        _vm.CommitCell(A("B2"), "4 * 5");
        _vm.Select(A("B2"));
        Assert.Contains("B2", _vm.StatusMessage);
        Assert.Contains("4 * 5", _vm.StatusMessage);
    }

    [Fact]
    public void ResizeCommands_ChangeSheetAndRaiseTableChanged()
    {
        var raised = 0;
        _vm.TableChanged += (_, _) => raised++;

        _vm.AddRowCommand.Execute(null);
        _vm.AddColumnCommand.Execute(null);
        Assert.Equal(11, _vm.Sheet.RowCount);
        Assert.Equal(11, _vm.Sheet.ColumnCount);

        _vm.RemoveRowCommand.Execute(null);
        _vm.RemoveColumnCommand.Execute(null);
        Assert.Equal(10, _vm.Sheet.RowCount);
        Assert.Equal(10, _vm.Sheet.ColumnCount);
        Assert.Equal(4, raised);
        Assert.True(_vm.IsDirty);
    }

    [Fact]
    public void RemoveRow_AtMinimumSize_ShowsMessage()
    {
        for (var i = 0; i < 9; i++)
        {
            _vm.RemoveRowCommand.Execute(null);
        }
        Assert.Equal(1, _vm.Sheet.RowCount);

        _vm.RemoveRowCommand.Execute(null);

        Assert.Equal(1, _vm.Sheet.RowCount);
        Assert.Contains("хоча б один рядок", _vm.StatusMessage);
    }

    [Fact]
    public async Task Save_ToLocalStorage()
    {
        _vm.CommitCell(A("A1"), "5");
        _dialogs.Choices.Enqueue(MainViewModel.LocalOption);
        _dialogs.Prompts.Enqueue("lab");

        await _vm.SaveCommand.ExecuteAsync();

        Assert.True(_local.Files.ContainsKey("lab"));
        Assert.Equal(0, _drive.Files.Count);
        Assert.False(_vm.IsDirty);
        Assert.Equal("lab", _vm.FileName);
        Assert.Contains("збережено", _vm.StatusMessage);
    }

    [Fact]
    public async Task Save_ToGoogleDrive_UsesDriveStorage()
    {
        _dialogs.Choices.Enqueue(MainViewModel.DriveOption);
        _dialogs.Prompts.Enqueue("cloud");

        await _vm.SaveCommand.ExecuteAsync();

        Assert.True(_drive.Files.ContainsKey("cloud"));
        Assert.Equal(0, _local.Files.Count);
    }

    [Fact]
    public async Task Save_Cancelled_DoesNothing()
    {
        _vm.CommitCell(A("A1"), "5");

        await _vm.SaveCommand.ExecuteAsync();

        Assert.Equal(0, _local.Files.Count);
        Assert.True(_vm.IsDirty);
    }

    [Fact]
    public async Task Save_Failure_ShowsMessageAndKeepsDirty()
    {
        _vm.CommitCell(A("A1"), "5");
        _local.FailWith = new IOException("диск заповнений");
        _dialogs.Choices.Enqueue(MainViewModel.LocalOption);
        _dialogs.Prompts.Enqueue("lab");

        await _vm.SaveCommand.ExecuteAsync();

        Assert.Contains("диск заповнений", _vm.StatusMessage);
        Assert.True(_vm.IsDirty);
    }

    [Fact]
    public async Task Open_LoadsSheetAndRaisesTableChanged()
    {
        var source = new Spreadsheet(4, 3);
        source.SetExpression(A("A1"), "7");
        _local.Files["x"] = SpreadsheetSerializer.Serialize(source);

        var raised = 0;
        _vm.TableChanged += (_, _) => raised++;
        _dialogs.Choices.Enqueue(MainViewModel.LocalOption);
        _dialogs.Prompts.Enqueue("x");

        await _vm.OpenCommand.ExecuteAsync();

        Assert.Equal("7", _vm.GetDisplayText(A("A1")));
        Assert.Equal(4, _vm.Sheet.RowCount);
        Assert.Equal("x", _vm.FileName);
        Assert.False(_vm.IsDirty);
        Assert.Equal(1, raised);
    }

    [Fact]
    public async Task Open_MissingFile_ShowsMessage()
    {
        _dialogs.Choices.Enqueue(MainViewModel.LocalOption);
        _dialogs.Prompts.Enqueue("nope");

        await _vm.OpenCommand.ExecuteAsync();

        Assert.Contains("Не вдалося відкрити", _vm.StatusMessage);
    }

    [Fact]
    public async Task New_WhenDirtyAndDeclined_KeepsSheet()
    {
        _vm.CommitCell(A("A1"), "5");
        _dialogs.ConfirmAnswer = false;

        await _vm.NewCommand.ExecuteAsync();

        Assert.Equal(1, _dialogs.ConfirmCalls);
        Assert.Equal("5", _vm.GetDisplayText(A("A1")));
    }

    [Fact]
    public async Task New_WhenDirtyAndConfirmed_CreatesEmptySheet()
    {
        _vm.CommitCell(A("A1"), "5");

        await _vm.NewCommand.ExecuteAsync();

        Assert.Equal("", _vm.GetDisplayText(A("A1")));
        Assert.False(_vm.IsDirty);
    }

    [Fact]
    public async Task New_WhenClean_DoesNotAsk()
    {
        await _vm.NewCommand.ExecuteAsync();
        Assert.Equal(0, _dialogs.ConfirmCalls);
    }

    [Fact]
    public async Task ShowHelp_OpensHelp()
    {
        await _vm.ShowHelpCommand.ExecuteAsync();
        Assert.Equal(1, _dialogs.HelpCalls);
    }
}
