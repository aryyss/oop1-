using SpreadsheetApp.Core.Model;
using SpreadsheetApp.Core.ViewModels;

namespace SpreadsheetApp.Views;

public partial class MainPage : ContentPage
{
    private const double CellWidth = 140;
    private const double CellHeight = 38;
    private const double HeaderWidth = 44;

    private static readonly Color LineColor = Color.FromArgb("#1F1F1F");
    private static readonly Color HeaderBackground = Color.FromArgb("#0A0A0A");
    private static readonly Color HeaderText = Color.FromArgb("#7A7A7A");
    private static readonly Color CellText = Color.FromArgb("#F2F2F2");
    private static readonly Color ErrorText = Color.FromArgb("#FF7A7A");
    private static readonly Color ActiveBackground = Color.FromArgb("#141414");

    private readonly MainViewModel _viewModel;
    private readonly Dictionary<CellAddress, Entry> _entries = new();
    private readonly HashSet<CellAddress> _editing = new();

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = viewModel;

        viewModel.TableChanged += (_, _) => MainThread.BeginInvokeOnMainThread(BuildTable);
        viewModel.ValuesChanged += (_, _) => MainThread.BeginInvokeOnMainThread(RefreshValues);

        BuildTable();
    }

    private void BuildTable()
    {
        TableGrid.Children.Clear();
        TableGrid.RowDefinitions.Clear();
        TableGrid.ColumnDefinitions.Clear();
        TableGrid.BackgroundColor = LineColor;
        TableGrid.ColumnSpacing = 1;
        TableGrid.RowSpacing = 1;
        TableGrid.Padding = new Thickness(1);
        _entries.Clear();
        _editing.Clear();

        var sheet = _viewModel.Sheet;

        TableGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(HeaderWidth)));
        for (var column = 0; column < sheet.ColumnCount; column++)
        {
            TableGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(CellWidth)));
        }

        TableGrid.RowDefinitions.Add(new RowDefinition(new GridLength(CellHeight)));
        for (var row = 0; row < sheet.RowCount; row++)
        {
            TableGrid.RowDefinitions.Add(new RowDefinition(new GridLength(CellHeight)));
        }

        TableGrid.Add(CreateHeader(string.Empty), 0, 0);

        for (var column = 0; column < sheet.ColumnCount; column++)
        {
            var letters = new CellAddress(column, 0).ToString().TrimEnd('1');
            TableGrid.Add(CreateHeader(letters), column + 1, 0);
        }

        for (var row = 0; row < sheet.RowCount; row++)
        {
            TableGrid.Add(CreateHeader((row + 1).ToString()), 0, row + 1);

            for (var column = 0; column < sheet.ColumnCount; column++)
            {
                var address = new CellAddress(column, row);
                var entry = CreateEntry(address);
                _entries[address] = entry;
                TableGrid.Add(entry, column + 1, row + 1);
            }
        }

        RefreshValues();
    }

    private static Label CreateHeader(string text) => new()
    {
        Text = text,
        FontAttributes = FontAttributes.Bold,
        FontSize = 12,
        HorizontalTextAlignment = TextAlignment.Center,
        VerticalTextAlignment = TextAlignment.Center,
        BackgroundColor = HeaderBackground,
        TextColor = HeaderText
    };

    private Entry CreateEntry(CellAddress address)
    {
        var entry = new Entry
        {
            FontSize = 14,
            BackgroundColor = Colors.Black,
            TextColor = CellText,
            HorizontalTextAlignment = TextAlignment.End,
            VerticalTextAlignment = TextAlignment.Center
        };

        entry.Focused += (_, _) =>
        {
            _editing.Add(address);
            _viewModel.Select(address);
            entry.Text = _viewModel.GetEditText(address);
            entry.TextColor = CellText;
            entry.BackgroundColor = ActiveBackground;
        };

        entry.Unfocused += (_, _) =>
        {
            entry.BackgroundColor = Colors.Black;

            if (_editing.Remove(address))
            {
                _viewModel.CommitCell(address, entry.Text);
            }
        };

        entry.Completed += (_, _) => entry.Unfocus();

        return entry;
    }

    private void RefreshValues()
    {
        foreach (var (address, entry) in _entries)
        {
            if (_editing.Contains(address))
            {
                continue;
            }

            entry.Text = _viewModel.GetDisplayText(address);
            entry.TextColor = _viewModel.IsErrorShown(address) ? ErrorText : CellText;
        }
    }
}