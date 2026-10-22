using SpreadsheetApp.Core.Model;
using SpreadsheetApp.Core.ViewModels;

namespace SpreadsheetApp.Views;

public partial class MainPage : ContentPage
{
    private const double CellWidth = 110;
    private const double CellHeight = 38;
    private const double HeaderWidth = 44;

    private static readonly Color HeaderBackground = Color.FromArgb("#E3E3E3");

    private readonly MainViewModel _viewModel;
    private readonly Dictionary<CellAddress, Entry> _entries = new();

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
        _entries.Clear();

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
        HorizontalTextAlignment = TextAlignment.Center,
        VerticalTextAlignment = TextAlignment.Center,
        BackgroundColor = HeaderBackground,
        TextColor = Colors.Black,
        Margin = new Thickness(1)
    };

    private Entry CreateEntry(CellAddress address)
    {
        var entry = new Entry
        {
            FontSize = 14,
            Margin = new Thickness(1),
            VerticalTextAlignment = TextAlignment.Center
        };

        entry.Focused += (_, _) =>
        {
            _viewModel.Select(address);
            entry.Text = _viewModel.GetEditText(address);
            entry.ClearValue(Entry.TextColorProperty);
        };

        entry.Unfocused += (_, _) => _viewModel.CommitCell(address, entry.Text);

        entry.Completed += (_, _) => entry.Unfocus();

        return entry;
    }

    private void RefreshValues()
    {
        foreach (var (address, entry) in _entries)
        {
            if (entry.IsFocused)
            {
                continue;
            }

            entry.Text = _viewModel.GetDisplayText(address);

            if (_viewModel.IsErrorShown(address))
            {
                entry.TextColor = Colors.Red;
            }
            else
            {
                entry.ClearValue(Entry.TextColorProperty);
            }
        }
    }
}
