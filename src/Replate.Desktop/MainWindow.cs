using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Replate.Domain.Entities;
using Menu = Replate.Domain.Entities.Menu;

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
    private readonly string? startupError;
    internal string? SuccessMessage => successBanner.IsVisible ? successText.Text : null;
    private TextBlock? alert;
    private Button? cartLink;

    public MainWindow(DemoSession? session = null, string? startupError = null)
    {
        Session = session ?? new DemoSession(DemoSession.DefaultStorePath);
        this.startupError = startupError;
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
        ShowHome();
    }

    // Accounts
    internal void ShowSignIn() => ShowPublic(new SignInView(this, Session, startupError));
    internal void ShowRegister() => ShowPublic(new RegisterView(this, Session));

    internal void ShowHome()
    {
        if (Session.CurrentUser is RestaurantOwner) ShowDashboard();
        else if (Session.CurrentUser is Customer) ShowMarketplace();
        else ShowSignIn();
    }

    internal void SignOut()
    {
        Session.SignOut();
        successTimer.Stop(); successBanner.IsVisible = false;
        ShowSignIn();
    }

    private void ShowPublic(Control page)
    {
        CurrentPage = page;
        cartLink = null; alert = null;
        pageHost.Content = page;
    }

    private bool Allowed(bool restaurant, bool owned = true)
    {
        var allowed = owned && (restaurant ? Session.CurrentUser is RestaurantOwner owner && Session.Restaurant.OwnerId == owner.Id
            : Session.CurrentUser is Customer);
        if (!allowed) { if (Session.IsSignedIn) Session.SignOut(); ShowSignIn(); }
        return allowed;
    }

    private bool OwnsRestaurant(Guid restaurantId) => Session.Restaurant.Id == restaurantId;

    // Customer area
    internal void ShowMarketplace()
    {
        if (!Allowed(false)) return;
        Marketplace = new MarketplaceView(this, Session);
        Render(false, Marketplace, "Jelajah");
        Marketplace.Refresh();
    }
    internal void ShowFood(SurplusListing listing)
    {
        if (Allowed(false)) Render(false, new FoodDetailView(this, Session, listing), "Jelajah");
    }
    internal void ShowCart()
    {
        if (Allowed(false)) Render(false, new CartView(this, Session), "Keranjang");
    }
    internal void ShowCustomerOrders()
    {
        if (Allowed(false)) Render(false, new OrdersView(this, Session, false), "Pesanan saya");
    }
    internal void ShowOrder(Order order, bool restaurant)
    {
        var owned = restaurant ? OwnsRestaurant(order.RestaurantId) : Session.CurrentUser?.Id == order.CustomerId;
        if (Allowed(restaurant, owned))
            Render(restaurant, new OrderDetailView(this, Session, order, restaurant), restaurant ? "Pesanan" : "Pesanan saya");
    }

    // Restaurant area
    internal void ShowDashboard()
    {
        if (Allowed(true)) Render(true, new DashboardView(this, Session), "Dashboard");
    }
    internal void ShowMenus()
    {
        if (Allowed(true)) Render(true, new MenusView(this, Session), "Menu");
    }
    internal void ShowMenuEditor(Menu? menu = null)
    {
        if (Allowed(true, menu is null || OwnsRestaurant(menu.RestaurantId)))
            Render(true, new MenuEditorView(this, Session, menu), "Menu");
    }
    internal void ShowListings()
    {
        if (Allowed(true)) Render(true, new ListingsView(this, Session), "Penawaran surplus");
    }
    internal void ShowListingEditor(SurplusListing? listing = null)
    {
        if (Allowed(true, listing is null || OwnsRestaurant(listing.Menu.RestaurantId)))
            Render(true, new ListingEditorView(this, Session, listing), "Penawaran surplus");
    }
    internal void ShowProduction()
    {
        if (Allowed(true)) Render(true, new ProductionView(this, Session), "Produksi");
    }
    internal void ShowRestaurantOrders()
    {
        if (Allowed(true)) Render(true, new OrdersView(this, Session, true), "Pesanan");
    }

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
            var signOut = Ui.Button("Keluar", SignOut);
            var top = new Border { Child = Ui.Columns("*,Auto", links, signOut),
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
            var footer = Ui.Stack(12, Ui.Text(Session.Restaurant.Name, 14, bold: true), Ui.Button("Keluar", SignOut));
            var dock = new DockPanel { Margin = new Thickness(20, 28, 20, 24) };
            DockPanel.SetDock(footer, Dock.Bottom); dock.Children.Add(footer); dock.Children.Add(nav);
            pageHost.Content = Ui.Columns("208,*", new Border { Child = dock, Background = Brush.Parse("#F1F3EE"),
                BorderBrush = Brush.Parse("#DDE3DA"), BorderThickness = new Thickness(0, 0, 1, 0) }, area);
        }
    }
}
