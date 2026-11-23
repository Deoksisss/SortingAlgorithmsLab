using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SortingLab.App.Services;
using SortingLab.App.ViewModels;
using SortingLab.App.Views;
using SortingLab.Core.Services;

namespace SortingLab.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        TempDirectoryManager.CleanupStaleDirectories();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow();
            FitToPrimaryScreen(window);
            var viewModel = new MainWindowViewModel(new FileDialogService(window));
            window.DataContext = viewModel;
            window.Closing += (_, _) => viewModel.Shutdown();
            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void FitToPrimaryScreen(MainWindow window)
    {
        var screen = window.Screens.Primary;
        if (screen is null || screen.Scaling <= 0)
        {
            return;
        }

        var availableWidth = screen.WorkingArea.Width / screen.Scaling;
        var availableHeight = screen.WorkingArea.Height / screen.Scaling;
        window.Width = Math.Min(1480, Math.Max(1000, availableWidth - 24));
        window.Height = Math.Min(900, Math.Max(640, availableHeight - 48));
    }
}
