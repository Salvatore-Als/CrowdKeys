using System.Text.Json;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using CrowdKeys.Models;
using CrowdKeys.ViewModels;
using CrowdKeys.Views;

namespace CrowdKeys;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;

            var config = LoadGlobalConfig();

            if (!string.IsNullOrEmpty(config.LastUserId))
                OpenMainWindow(desktop, null, null);
            else
                OpenLoginWindow(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void OpenLoginWindow(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var vm  = new LoginWindowViewModel();
        var win = new LoginWindow { DataContext = vm };

        vm.LoginSucceeded += (_, tokens) =>
            Dispatcher.UIThread.Post(() =>
            {
                OpenMainWindow(desktop, tokens.AccessToken, tokens.RefreshToken);
                win.Close();
            });

        desktop.MainWindow = win;
        win.Show();
    }

    private static void OpenMainWindow(
        IClassicDesktopStyleApplicationLifetime desktop,
        string? accessToken,
        string? refreshToken)
    {
        var vm  = new MainWindowViewModel(accessToken, refreshToken);
        var win = new MainWindow { DataContext = vm };

        vm.LoggedOut += (_, _) =>
            Dispatcher.UIThread.Post(() =>
            {
                OpenLoginWindow(desktop);
                win.Close();
            });

        EventHandler<Avalonia.Controls.WindowClosingEventArgs>? closingHandler = null;
        closingHandler = (_, _) =>
        {
            if (desktop.MainWindow == win)
            {
                win.Closing -= closingHandler;
                desktop.Shutdown();
            }
        };
        win.Closing += closingHandler;

        desktop.MainWindow = win;
        win.Show();
    }

    private static GlobalConfig LoadGlobalConfig()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CrowdKeys", "config.json");
        try
        {
            if (File.Exists(path))
                return JsonSerializer.Deserialize<GlobalConfig>(File.ReadAllText(path)) ?? new();
        }
        catch { }
        return new();
    }
}
