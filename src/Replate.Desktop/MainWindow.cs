using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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
    internal CustomerTopBar TopBar { get; }
    internal const double RailLogoHeight = 32;
    internal Image? RailLogo { get; private set; }
    private readonly ContentControl customerArea = new();
    private readonly DockPanel customerShell = new();
    private readonly Panel dialogLayer = new();
    private ModalDialog? modal;
    internal FoodDialog? Dialog => modal as FoodDialog;
    internal CheckoutDialog? Confirmation => modal as CheckoutDialog;
    internal string SearchText => TopBar.SearchBox.Text?.Trim() ?? "";

    public MainWindow(DemoSession? session = null, string? startupError = null)
    {
        Session = session ?? new DemoSession(DemoSession.DefaultStorePath);
        this.startupError = startupError;
        Title = "Replate"; Width = 1360; Height = 940; MinWidth = 1040; MinHeight = 720;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = Ui.CanvasFill;
        FontFamily = new FontFamily("Helvetica Neue, Segoe UI, sans-serif");
        successBanner = new Border
        {
            Child = successText, IsVisible = false,
            Background = Brush.Parse("#E0E9DF"), BorderBrush = Ui.Green,
            BorderThickness = new Thickness(4, 0, 0, 0), CornerRadius = new CornerRadius(6),
            Padding = new Thickness(16, 12), Margin = new Thickness(32, 16, 32, 0)
        };
        TopBar = new CustomerTopBar(ShowMarketplace, ShowCart, ShowCustomerOrders, SignOut, SubmitSearch);
        DockPanel.SetDock(TopBar, Dock.Top);
        customerShell.Children.Add(TopBar); customerShell.Children.Add(customerArea);
        customerShell.Styles.Add(Ui.FocusStyles());
        var shell = new DockPanel();
        DockPanel.SetDock(successBanner, Dock.Top);
        shell.Children.Add(successBanner);
        shell.Children.Add(pageHost);
        dialogLayer.Styles.Add(Ui.FocusStyles());
        Content = new Panel { Children = { shell, dialogLayer } };
        AddHandler(KeyDownEvent, (_, e) =>
        {
            if (e.Key != Key.Escape || modal is null) return;
            e.Handled = true;
            modal.Close();
        }, RoutingStrategies.Tunnel);
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
        TopBar.ProfileMenu.IsOpen = false;
        Session.SignOut();
        successTimer.Stop(); successBanner.IsVisible = false;
        ShowSignIn();
        TopBar.SearchBox.Text = "";
    }

    private void ShowPublic(Control page)
    {
        DismissDialog();
        CurrentPage = page;
        alert = null;
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
        Run(Session.RefreshSharedListings, Marketplace.Refresh);
    }
    internal void ShowCollection(string name)
    {
        if (!Allowed(false) || !MarketplaceView.CollectionNames.Contains(name)) return;
        var page = CollectionView.ForCollection(this, Session, name);
        Render(false, page, "Jelajah");
        page.Refresh();
    }
    internal void ShowSearch(string query)
    {
        var text = query.Trim();
        if (!Allowed(false) || text.Length == 0) return;
        var page = CollectionView.ForSearch(this, Session, text);
        Render(false, page, "Jelajah", text);
        page.Refresh();
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
        if (!Allowed(false)) return;
        Render(false, new OrdersView(this, Session, false), "Pesanan saya");
        Run(Session.RefreshCustomerOrders, () => Render(false, new OrdersView(this, Session, false), "Pesanan saya"));
    }
    internal void ShowOrder(Order order, bool restaurant)
    {
        var owned = restaurant
            ? OwnsRestaurant(order.RestaurantId) && Session.CanViewRestaurantOrder(order)
            : Session.CurrentUser?.Id == order.CustomerId;
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
        if (!Allowed(true)) return;
        Render(true, new MenusView(this, Session), "Menu");
        Run(Session.RefreshRestaurantMenus, () => Render(true, new MenusView(this, Session), "Menu"));
    }
    internal void ShowRestaurantProfile()
    {
        if (Allowed(true)) Render(true, new RestaurantProfileView(this, Session), "Dashboard");
    }
    internal void ShowMenuEditor(Menu? menu = null)
    {
        if (Allowed(true, menu is null || OwnsRestaurant(menu.RestaurantId)))
            Render(true, new MenuEditorView(this, Session, menu), "Menu");
    }
    internal void ShowListings()
    {
        if (!Allowed(true)) return;
        Render(true, new ListingsView(this, Session), "Penawaran surplus");
        Run(Session.RefreshSharedListings, () => Render(true, new ListingsView(this, Session), "Penawaran surplus"));
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
        if (!Allowed(true)) return;
        Run(Session.RefreshRestaurantOrders, ShowList, onError: message =>
        {
            ShowList();
            if (alert is null) return;
            alert.Text = message;
            alert.IsVisible = true;
        });

        void ShowList() => Render(true, new OrdersView(this, Session, true), "Pesanan");
    }

    // Dialog
    internal void ShowFoodDialog(SurplusListing listing, Func<SurplusListing, int, string?> add, bool keyboard, Action? focusBack)
    {
        if (Session.CurrentUser is Customer) ShowModal(new FoodDialog(Session, listing, add, keyboard), focusBack);
    }

    internal void ShowCheckoutDialog(CartRestaurantGroup group, Func<Task<string?>> submit, bool keyboard, Action? focusBack)
    {
        if (Session.CurrentUser is not Customer || Session.FindRestaurant(group.RestaurantId) is not { } restaurant) return;
        ShowModal(new CheckoutDialog(restaurant.Name, group, submit, keyboard), focusBack);
    }

    private void ShowModal(ModalDialog dialog, Action? focusBack)
    {
        DismissDialog();
        dialog.Closed += () =>
        {
            if (modal != dialog) return;
            DismissDialog();
            focusBack?.Invoke();
        };
        modal = dialog;
        dialogLayer.Children.Add(dialog);
    }

    private void DismissDialog()
    {
        modal = null;
        dialogLayer.Children.Clear();
    }

    internal void UpdateCartCount() => TopBar.SetCartCount(Session.Cart.Items.Sum(i => i.Quantity));

    internal void SubmitSearch(string query)
    {
        if (!string.IsNullOrWhiteSpace(query)) ShowSearch(query);
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

    internal void ShowSuccess(string message)
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
    private void Render(bool restaurant, Control page, string active, string search = "")
    {
        DismissDialog();
        CurrentPage = page;
        alert = Ui.Text(Session.StorageError ?? "", 14, Brush.Parse("#963E28"));
        alert.IsVisible = Session.StorageError is not null; alert.Margin = new Thickness(32, 16, 32, 0);
        var area = new DockPanel(); DockPanel.SetDock(alert, Dock.Top); area.Children.Add(alert); area.Children.Add(page);
        Button Nav(string label, Action route)
        {
            var button = Ui.Button(label, route);
            button.Background = label == active ? Brush.Parse("#E0E9DF") : Brushes.Transparent;
            button.Foreground = label == active ? Ui.Green : Ui.Muted;
            return button;
        }
        if (!restaurant)
        {
            TopBar.Show(Session.CurrentUser, Session.Cart.Items.Sum(i => i.Quantity), active == "Keranjang");
            TopBar.SearchBox.Text = search;
            customerArea.Content = area;
            pageHost.Content = customerShell;
        }
        else
        {
            RailLogo = Ui.Logo(Ui.LogoGreen, RailLogoHeight);
            RailLogo.Margin = new Thickness(12, 0, 0, 32);
            var nav = Ui.Stack(8, RailLogo, Nav("Dashboard", ShowDashboard), Nav("Menu", ShowMenus),
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
