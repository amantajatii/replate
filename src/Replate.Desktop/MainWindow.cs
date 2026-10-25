using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Replate.Desktop;

public sealed class MainWindow : Window
{
    internal DemoSession Session { get; }
    internal MarketplaceView Marketplace { get; private set; } = null!;
    internal Control? CurrentPage { get; private set; }
    private readonly ContentControl pageHost = new();
    private readonly TextBlock successText = Ui.Text("", 14, Ui.Green);
    private readonly Border successBanner;
    private readonly Avalonia.Threading.DispatcherTimer successTimer = new() { Interval = TimeSpan.FromSeconds(4) };
    internal string? SuccessMessage => successBanner.IsVisible ? successText.Text : null;
    private TextBlock? alert;
    private Button? cartLink;

    public MainWindow(DemoSession? session = null)
    {
        Session = session ?? new DemoSession(DemoSession.DefaultStorePath);
        Title = "Replate"; Width = 1360; Height = 940; MinWidth = 1040; MinHeight = 720;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = Brush.Parse("#FAFAF8");
        FontFamily = new FontFamily("Helvetica Neue, Segoe UI, sans-serif");
        successBanner = new Border
        {
            Child = successText, IsVisible = false,
            Background = Brush.Parse("#E0E9DF"), BorderBrush = Ui.Green,
            BorderThickness = new Thickness(4, 0, 0, 0), CornerRadius = new CornerRadius(6),
            Padding = new Thickness(16, 12), Margin = new Thickness(32, 16, 32, 0)
        };
        var shell = new DockPanel();
        DockPanel.SetDock(successBanner, Dock.Top);
        shell.Children.Add(successBanner);
        shell.Children.Add(pageHost);
        Content = shell;
        successTimer.Tick += (_, _) => { successTimer.Stop(); successBanner.IsVisible = false; };
        Closed += (_, _) => successTimer.Stop();
        ShowEntry();
    }

    internal void ShowEntry()
    {
        cartLink = null;
        var body = Ui.Stack(24, Ui.Text("replate.", 36, Ui.Green, true),
            Ui.Text("Pilih area Replate", 28, bold: true),
            Ui.Text("Jelajahi makanan surplus sebagai pelanggan atau kelola operasional restoran.", 16, Ui.Muted),
            Ui.Divider(),
            Ui.Stack(10, Ui.Text("Pelanggan", 20, bold: true),
                Ui.Text("Cari makanan, buat reservasi, dan pantau waktu pengambilan.", 14, Ui.Muted),
                Ui.Button("Jelajahi makanan", ShowMarketplace, true)),
            Ui.Divider(),
            Ui.Stack(10, Ui.Text("Pemilik restoran", 20, bold: true),
                Ui.Text("Kelola menu, penawaran, produksi, dan pesanan di Dapur Bumi.", 14, Ui.Muted),
                Ui.Button("Buka area restoran", ShowDashboard)),
            Ui.Text("Pilihan area untuk demonstrasi lokal. Tidak menggunakan autentikasi akun.", 12, Ui.Muted));
        body.MaxWidth = 600; body.HorizontalAlignment = HorizontalAlignment.Center;
        body.VerticalAlignment = VerticalAlignment.Center; body.Margin = new Thickness(40);
        pageHost.Content = Ui.Scroll(body);
    }

    internal void ShowMarketplace()
    {
        Marketplace = new MarketplaceView(this, Session);
        Render(false, Marketplace, "Jelajah");
        Marketplace.Refresh();
    }
    internal void ShowFood(Replate.Domain.Entities.SurplusListing listing) =>
        Render(false, new FoodDetailView(this, Session, listing), "Jelajah");
    internal void ShowCart() => Render(false, new CartView(this, Session), "Keranjang");
    internal void ShowCustomerOrders() => Render(false, new OrdersView(this, Session, false), "Pesanan saya");
    internal void ShowOrder(Replate.Domain.Entities.Order order, bool restaurant) =>
        Render(restaurant, new OrderDetailView(this, Session, order, restaurant), restaurant ? "Pesanan" : "Pesanan saya");
    internal void ShowDashboard() => Render(true, new DashboardView(Session), "Dashboard");
    internal void ShowMenus() => Render(true, new MenusView(this, Session), "Menu");
    internal void ShowMenuEditor(Replate.Domain.Entities.Menu? menu = null) =>
        Render(true, new MenuEditorView(this, Session, menu), "Menu");
    internal void ShowListings() => Render(true, new ListingsView(this, Session), "Penawaran surplus");
    internal void ShowListingEditor(Replate.Domain.Entities.SurplusListing? listing = null) =>
        Render(true, new ListingEditorView(this, Session, listing), "Penawaran surplus");
    internal void ShowProduction() => Render(true, new ProductionView(this, Session), "Produksi");
    internal void ShowRestaurantOrders() => Render(true, new OrdersView(this, Session, true), "Pesanan");

    internal void UpdateCartCount()
    {
        if (cartLink is not null) cartLink.Content = $"Keranjang ({Session.Cart.Items.Sum(i => i.Quantity)})";
    }

    internal void Run(Action action, Action? next = null, string? successMessage = null, Action<string>? onError = null)
    {
        try
        {
            action();
            if (alert is not null) alert.IsVisible = false;
            UpdateCartCount();
            next?.Invoke();
            if (successMessage is not null) ShowSuccess(successMessage);
        }
        catch (Exception e) when (e is InvalidOperationException or ArgumentException or IOException or UnauthorizedAccessException)
        {
            var message = e is IOException or UnauthorizedAccessException
                ? "Perubahan pada sesi ini belum tersimpan ke berkas lokal. " + e.Message : e.Message;
            if (onError is not null) onError(message);
            else if (alert is not null)
            {
                alert.Text = message;
                alert.IsVisible = true;
            }
        }
    }

    private void ShowSuccess(string message)
    {
        successTimer.Stop();
        successText.Text = message;
        successBanner.IsVisible = true;
        successTimer.Start();
    }

    internal async Task RunAsync(Func<Task> action, string successMessage, string failureMessage)
    {
        try
        {
            await action();
            if (alert is not null) alert.IsVisible = false;
            ShowSuccess(successMessage);
        }
        catch (Exception)
        {
            successTimer.Stop();
            successBanner.IsVisible = false;
            if (alert is not null) { alert.Text = failureMessage; alert.IsVisible = true; }
        }
    }
    private void Render(bool restaurant, Control page, string active)
    {
        CurrentPage = page;
        cartLink = null;
        alert = Ui.Text(Session.StorageError ?? "", 14, Brush.Parse("#963E28"));
        alert.IsVisible = Session.StorageError is not null; alert.Margin = new Thickness(32, 16, 32, 0);
        var area = new DockPanel(); DockPanel.SetDock(alert, Dock.Top); area.Children.Add(alert); area.Children.Add(page);
        var logo = Ui.Text("replate.", 28, Ui.Green, true);
        Button Nav(string label, Action route)
        {
            var button = Ui.Button(label, route);
            button.Background = label == active ? Brush.Parse("#E0E9DF") : Brushes.Transparent;
            button.Foreground = label == active ? Ui.Green : Ui.Muted;
            return button;
        }
        if (!restaurant)
        {
            cartLink = Nav("Keranjang", ShowCart); UpdateCartCount();
            var links = Ui.Row(12, logo, Nav("Jelajah", ShowMarketplace), Nav("Pesanan saya", ShowCustomerOrders), cartLink);
            var switchArea = Ui.Button("Ganti area", ShowEntry);
            var top = new Border { Child = Ui.Columns("*,Auto", links, switchArea),
                Padding = new Thickness(32, 16), Background = Brushes.White,
                BorderBrush = Brush.Parse("#DDE3DA"), BorderThickness = new Thickness(0, 0, 0, 1) };
            var root = new DockPanel(); DockPanel.SetDock(top, Dock.Top); root.Children.Add(top); root.Children.Add(area);
            pageHost.Content = root;
        }
        else
        {
            logo.Margin = new Thickness(12, 0, 0, 32);
            var nav = Ui.Stack(8, logo, Nav("Dashboard", ShowDashboard), Nav("Menu", ShowMenus),
                Nav("Penawaran surplus", ShowListings), Nav("Produksi", ShowProduction), Nav("Pesanan", ShowRestaurantOrders));
            foreach (var button in nav.Children.OfType<Button>()) button.HorizontalAlignment = HorizontalAlignment.Stretch;
            var footer = Ui.Stack(12, Ui.Text(Session.Restaurant.Name, 14, bold: true), Ui.Button("Ganti area", ShowEntry));
            var dock = new DockPanel { Margin = new Thickness(20, 28, 20, 24) };
            DockPanel.SetDock(footer, Dock.Bottom); dock.Children.Add(footer); dock.Children.Add(nav);
            pageHost.Content = Ui.Columns("208,*", new Border { Child = dock, Background = Brush.Parse("#F1F3EE"),
                BorderBrush = Brush.Parse("#DDE3DA"), BorderThickness = new Thickness(0, 0, 1, 0) }, area);
        }
    }
}
