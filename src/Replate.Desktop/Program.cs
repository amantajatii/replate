using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Replate.Desktop.Data;

namespace Replate.Desktop;

internal static class Program
{
    [STAThread]
    public static async Task Main(string[] args)
    {
        if (args.Contains("--verify-state")) { StateVerification.Run(); MultiRestaurantVerification.Run(); AccountVerification.Run(); return; }

        string? databaseError = null;
        if (!args.Contains("--verify-ui"))
        {
            try
            {
                await new DatabaseService().InitializeAsync();
            }
            catch (Exception e) when (e is InvalidOperationException or ArgumentException)
            {
                databaseError = e.Message;
            }
        }

        App.DatabaseError = databaseError;
        AppBuilder.Configure<App>().UsePlatformDetect().StartWithClassicDesktopLifetime(args);
    }
}

public sealed class App : Application
{
    internal static string? DatabaseError { get; set; }

    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Light;
        Styles.Add(new FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var args = desktop.Args ?? [];
            var window = new MainWindow(args.Contains("--verify-ui") ? new DemoSession(Path.Combine(Path.GetTempPath(), "replate-ui-" + Guid.NewGuid().ToString("N"), "state.json")) : null, DatabaseError);
            desktop.MainWindow = window;
            if (args.Contains("--verify-ui"))
                window.Opened += async (_, _) => await UiVerification.Run(window, desktop);
        }
        base.OnFrameworkInitializationCompleted();
    }
}
