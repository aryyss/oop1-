using SpreadsheetApp.Views;

namespace SpreadsheetApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(HelpPage), typeof(HelpPage));
    }
}