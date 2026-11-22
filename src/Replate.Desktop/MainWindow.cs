using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
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
    private Customer? locationLoading;
    private int locationRun;
    private bool closed;
    internal FoodDialog? Dialog => modal as FoodDialog;
    internal CheckoutDialog? Confirmation => modal as CheckoutDialog;
    internal LocationBrowserDialog? LocationConfirmation => modal as LocationBrowserDialog;
    internal string SearchText => TopBar.SearchBox.Text?.Trim() ?? "";

    public MainWindow(DemoSession? session = null, string? startupError = null)
    {
        Session = session ?? new DemoSession(DemoSession.DefaultStorePath);
        if (MapView.OfflineTiles) VerificationGeoFakes.Configure(Session);
        this.startupError = startupError;
        Title = "Replate"; Width = 1360; Height = 940; MinWidth = 1040; MinHeight = 720;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = Ui.Canvas;
        FontFamily = new FontFamily("Helvetica Neue, Segoe UI, sans-serif");
        successBanner = new Border
        {
            Child = successText, IsVisible = false,
            Background = Ui.Selection, BorderBrush = Ui.Green,
            BorderThickness = new Thickness(4, 0, 0, 0), CornerRadius = new CornerRadius(6),
            Padding = new Thickness(16, 12), Margin = new Thickness(32, 16, 32, 0)
        };
        TopBar = new CustomerTopBar(ShowMarketplace, ShowCart, ShowCustomerOrders, SignOut, SubmitSearch,
            Session, RefreshCustomerTravelAsync, ConfirmBrowserLocation, ShowSuccess);
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
        Closed += (_, _) => { closed = true; TopBar.CloseLocationPicker(); DismissDialog(); successTimer.Stop(); };
        ShowHome();
    }

    // Accounts
    internal void ShowSignIn() => ShowPublic(new SignInView(this, Session, startupError));
    internal void ShowRegister() => ShowPublic(new RegisterView(this, Session));

    internal void ShowHome()
    {
        if (Session.CurrentUser is RestaurantOwner) ShowDashboard();
        else if (Session.CurrentCustomer is { } customer)
        {
            ShowMarketplace();
            _ = LoadCustomerLocationAsync(customer);
        }
        else ShowSignIn();
    }

    internal void SignOut()
    {
        TopBar.ProfileMenu.IsOpen = false;
        TopBar.CloseLocationPicker();
        TopBar.LocationInput.Text = "";
        locationLoading = null;
        locationRun++;
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
        if (!allowed)
        {
            if (Session.IsSignedIn) SignOut();
            else ShowSignIn();
        }
        return allowed;
    }

    private bool OwnsRestaurant(Guid restaurantId) => Session.Restaurant.Id == restaurantId;

    private async Task LoadCustomerLocationAsync(Customer customer)
    {
        if (locationLoading == customer) return;
        locationLoading = customer;
        var run = ++locationRun;
        try
        {
            await Task.Yield();
            if (!IsCurrentCustomer(customer) || run != locationRun) return;
            try { await Session.EnsureCustomerLocationAsync(); }
            catch (Exception error) when (error is InvalidOperationException or IOException or UnauthorizedAccessException) { }

            if (!IsCurrentCustomer(customer) || run != locationRun) return;
            TopBar.ShowLocation(customer);
            try { await Session.EnsureRestaurantLocationsAsync(); }
            catch (Exception error) when (error is InvalidOperationException or IOException or UnauthorizedAccessException) { }

            if (IsCurrentCustomer(customer) && run == locationRun) await RefreshCustomerTravelAsync();
        }
        finally
        {
            if (run == locationRun) locationLoading = null;
        }
    }

    private bool IsCurrentCustomer(Customer customer) => !closed && Session.CurrentCustomer == customer;

    private async Task RefreshCustomerTravelAsync()
    {
        if (Session.CurrentCustomer is not { } customer || !IsCurrentCustomer(customer)) return;
        var run = locationRun;
        TopBar.ShowLocation(customer);
        await Session.RefreshTravelAsync();
        if (!IsCurrentCustomer(customer) || run != locationRun) return;
        if (CurrentPage is MarketplaceView marketplace) marketplace.Refresh();
        if (CurrentPage is CollectionView collection) collection.Refresh();
        Dialog?.Update();
    }

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
        if (Allowed(true)) Render(true, new MenusView(this, Session), "Menu");
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
    internal void ShowRestaurantProfile()
    {
        if (Allowed(true)) Render(true, new RestaurantProfileView(this, Session), "Profil");
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

    private Task<bool> ConfirmBrowserLocation()
    {
        var confirmation = new LocationBrowserDialog();
        var answer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        ShowModal(confirmation, null);
        confirmation.Closed += () => answer.TrySetResult(confirmation.Accepted);
        return answer.Task;
    }

    private void DismissDialog()
    {
        var dismissed = modal;
        modal = null;
        dialogLayer.Children.Clear();
        dismissed?.Close();
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
        if (!restaurant)
        {
            TopBar.Show(Session.CurrentUser, Session.Cart.Items.Sum(i => i.Quantity), active == "Keranjang");
            TopBar.SearchBox.Text = search;
            customerArea.Content = area;
            pageHost.Content = customerShell;
        }
        else
        {
            Button Nav(string label, string glyph, Action route)
            {
                var button = Ui.Button(label, route);
                button.ContentTemplate = new FuncDataTemplate<string>((text, _) =>
                {
                    var icon = Ui.OutlineIcon(glyph, 18, .085, null);
                    icon.Bind(Shape.StrokeProperty, button.GetObservable(Button.ForegroundProperty));
                    return Ui.Row(10, icon, new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center });
                });
                AutomationProperties.SetName(button, label);
                button.ClearValue(Button.BackgroundProperty);
                button.ClearValue(Button.ForegroundProperty);
                button.BorderThickness = default;
                button.CornerRadius = new CornerRadius(8);
                button.MinHeight = 44;
                button.Padding = new Thickness(12, 8);
                button.HorizontalAlignment = HorizontalAlignment.Stretch;
                button.HorizontalContentAlignment = HorizontalAlignment.Left;
                button.Classes.Add(Ui.OnPrimaryClass);
                button.Classes.Set("active", label == active);
                return button;
            }
            RailLogo = Ui.Logo(Ui.LogoWhite, RailLogoHeight);
            RailLogo.Margin = new Thickness(12, 0, 0, 28);
            var nav = Ui.Stack(6, RailLogo,
                Nav("Dashboard", "M3 3H10V10H3Z M14 3H21V10H14Z M3 14H10V21H3Z M14 14H21V21H14Z", ShowDashboard),
                Nav("Menu", "M4 4H10Q12 4 12 6Q12 4 14 4H20V19H14Q12 19 12 21Q12 19 10 19H4Z M12 6V20", ShowMenus),
                Nav("Penawaran surplus", "M3 12V4H11L21 14L14 21Z M7.5 7.5H8", ShowListings),
                Nav("Produksi", "M4 11H20L18 20H6Z M3 11H21 M8 7C6 5 10 4 8 2 M14 7C12 5 16 4 14 2", ShowProduction),
                Nav("Pesanan", Ui.ReceiptOutline, ShowRestaurantOrders),
                Nav("Profil", MarketplaceView.PinOutline, ShowRestaurantProfile));
            var restaurantName = Ui.Text(Session.Restaurant.Name, 14, Ui.OnPrimary, bold: true);
            restaurantName.MaxLines = 2;
            restaurantName.TextTrimming = TextTrimming.CharacterEllipsis;
            ToolTip.SetTip(restaurantName, Session.Restaurant.Name);
            var shopIcon = Ui.OutlineIcon("M3 10L5 4H19L21 10 M3 10H21 M5 10V20H19V10 M10 20V14H14V20", 18, .085, Ui.Green);
            AutomationProperties.SetAccessibilityView(shopIcon, AccessibilityView.Raw);
            var shop = new Border
            {
                Width = 34, Height = 34, CornerRadius = new CornerRadius(8), Background = Ui.Selection, Child = shopIcon
            };
            var identity = Ui.Columns("Auto,*", shop, restaurantName);
            identity.ColumnSpacing = 10;
            var logout = Nav("Keluar", "M11 4H4V20H11 M9 12H21 M17 8L21 12L17 16", SignOut);
            logout.MinHeight = 38;
            logout.Padding = new Thickness(8, 6);
            var footer = new Border
            {
                Background = Brush.Parse("#1AFFFFFF"), BorderBrush = Brush.Parse("#26FFFFFF"), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12), Padding = new Thickness(8), Child = Ui.Stack(6, identity, logout)
            };
            var dock = new DockPanel { Margin = new Thickness(20, 28, 20, 24) };
            dock.Styles.Add(Ui.FocusStyles());
            dock.Styles.Add(new Style(x => x.OfType<Button>().Class(Ui.OnPrimaryClass))
            { Setters = { new Setter(Button.BackgroundProperty, Brushes.Transparent), new Setter(Button.ForegroundProperty, Ui.OnPrimary) } });
            foreach (var state in new[] { "active", ":pointerover", ":pressed" })
                dock.Styles.Add(new Style(x => x.OfType<Button>().Class(Ui.OnPrimaryClass).Class(state))
                { Setters = { new Setter(Button.BackgroundProperty, Ui.Selection), new Setter(Button.ForegroundProperty, Ui.Green) } });
            DockPanel.SetDock(footer, Dock.Bottom); dock.Children.Add(footer); dock.Children.Add(nav);
            pageHost.Content = Ui.Columns("224,*", new Border { Child = dock, Background = Ui.Green }, area);
        }
    }
}
