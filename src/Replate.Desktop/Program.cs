using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Replate.Desktop.Data;

namespace Replate.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Contains("--verify-state"))
        {
            StateVerification.Run();
            MultiRestaurantVerification.Run();
            AccountVerification.Run();
            LocationStateVerification.Run();
            return;
        }

        string? databaseError = null;
        if (!args.Contains("--verify-ui"))
        {
            try
            {
                new DatabaseService().InitializeAsync().GetAwaiter().GetResult();
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
        Ui.UseInputs(this);
        Ui.UsePrimaryButtons(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var args = desktop.Args ?? [];
            MapView.OfflineTiles = args.Contains("--verify-ui");
            var verificationSession = MapView.OfflineTiles ? new DemoSession(
                Path.Combine(Path.GetTempPath(), "replate-ui-" + Guid.NewGuid().ToString("N"), "state.json")) : null;
            var window = new MainWindow(verificationSession, DatabaseError);
            desktop.MainWindow = window;
            var expirationTimer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
            expirationTimer.Tick += (_, _) => ExpireOrders();
            window.Opened += (_, _) =>
            {
                ExpireOrders();
                expirationTimer.Start();
            };
            window.Closed += (_, _) => expirationTimer.Stop();

            void ExpireOrders()
            {
                try { window.Session.ExpireOverdueOrders(DateTime.UtcNow); }
                catch (Exception e) when (e is InvalidOperationException or IOException or UnauthorizedAccessException)
                { System.Diagnostics.Trace.TraceError("Gagal menutup pesanan yang lewat jadwal: {0}", e); }
            }

            if (args.Contains("--verify-ui"))
                window.Opened += async (_, _) => await UiVerification.Run(window, desktop);
        }
        base.OnFrameworkInitializationCompleted();
    }
}
