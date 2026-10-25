using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

namespace Replate.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Contains("--verify-state")) { StateVerification.Run(); return; }
        AppBuilder.Configure<App>().UsePlatformDetect().StartWithClassicDesktopLifetime(args);
    }
}

public sealed class App : Application
{
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
            var window = new MainWindow(args.Contains("--verify-ui") ? new DemoSession(Path.Combine(Path.GetTempPath(), "replate-ui-" + Guid.NewGuid().ToString("N"), "state.json")) : null);
            desktop.MainWindow = window;
            if (args.Contains("--verify-ui"))
                window.Opened += async (_, _) => await UiVerification.Run(window, desktop);
        }
        base.OnFrameworkInitializationCompleted();
    }
}
