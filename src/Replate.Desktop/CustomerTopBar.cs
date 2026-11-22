using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Path = Avalonia.Controls.Shapes.Path;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Replate.Domain.Entities;

namespace Replate.Desktop;

internal sealed class CustomerTopBar : Border
{
    internal const double BarHeight = 72;
    internal const double LogoHeight = 36;
    internal const double SearchMaxWidth = 720;
    internal const double SearchMinWidth = 320;
    internal const string SearchPlaceholder = "Mulai cari menu atau restoran";
    internal static readonly IBrush Selection = Ui.Selection;
    internal static readonly Geometry SearchIcon = Geometry.Parse("M10.5 4A6.5 6.5 0 1 1 10.5 17A6.5 6.5 0 1 1 10.5 4Z M15.5 15.5L20 20");
    internal static readonly Geometry CartIcon = Geometry.Parse(
        "M3 4H5.5L7.6 14.4A1.5 1.5 0 0 0 9.1 15.6H17.6A1.5 1.5 0 0 0 19.1 14.4L20.5 8H6.4 M9.5 19A1 1 0 1 1 9.5 21A1 1 0 1 1 9.5 19Z M17 19A1 1 0 1 1 17 21A1 1 0 1 1 17 19Z");
    internal static readonly Geometry UserIcon = Geometry.Parse("M12 4A4 4 0 1 1 12 12A4 4 0 1 1 12 4Z M4.5 20.5C5.3 16.8 8.3 14.5 12 14.5C15.7 14.5 18.7 16.8 19.5 20.5");
    private static readonly ControlTheme PlainButton = new(typeof(Button))
    {
        Setters =
        {
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<Button>((button, _) => Ui.FocusFrame(new Border
            {
                [!Border.BackgroundProperty] = button[!TemplatedControl.BackgroundProperty],
                [!Border.BorderBrushProperty] = button[!TemplatedControl.BorderBrushProperty],
                [!Border.BorderThicknessProperty] = button[!TemplatedControl.BorderThicknessProperty],
                [!Border.CornerRadiusProperty] = button[!TemplatedControl.CornerRadiusProperty],
                [!Border.PaddingProperty] = button[!TemplatedControl.PaddingProperty],
                Child = new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [!ContentPresenter.ContentProperty] = button[!ContentControl.ContentProperty],
                    [!ContentPresenter.ForegroundProperty] = button[!TemplatedControl.ForegroundProperty],
                    [!ContentPresenter.HorizontalAlignmentProperty] = button[!ContentControl.HorizontalContentAlignmentProperty],
                    VerticalAlignment = VerticalAlignment.Center
                }
            }, button.CornerRadius)))
        }
    };

    internal Button LogoButton { get; }
    internal Image Logo { get; }
    internal TextBox SearchBox { get; }
    internal Button CartButton { get; }
    internal Button SearchButton { get; }
    internal Path SearchGlyph { get; }
    internal Path CartGlyph { get; }
    internal Border CartBadge { get; }
    internal Border CartIndicator { get; }
    internal Panel SearchFrame { get; }
    internal Border SearchRing { get; }
    internal TextBlock CartBadgeText { get; }
    internal Button ProfileButton { get; }
    internal Popup ProfileMenu { get; }
    internal Border MenuCard { get; }
    internal TextBlock NameText { get; }
    internal TextBlock EmailText { get; }
    internal Button OrdersItem { get; }
    internal Button SignOutItem { get; }

    internal CustomerTopBar(Action home, Action cart, Action orders, Action signOut, Action<string> search)
    {
        Height = BarHeight; Background = Ui.Green; Padding = new Thickness(Ui.Spacing.Page, 0);
        AddStyles();

        // Logo
        Logo = Ui.Logo(Ui.LogoWhite, LogoHeight);
        LogoButton = Plain("bar-logo", Logo, home);
        LogoButton.Padding = default;
        AutomationProperties.SetName(LogoButton, Ui.LogoName);
        ToolTip.SetTip(LogoButton, "Jelajah");

        // Search
        SearchBox = new TextBox
        {
            PlaceholderText = SearchPlaceholder, Height = 44, CornerRadius = new CornerRadius(9999), Background = Brushes.White,
            Foreground = Ui.Ink, PlaceholderForeground = Ui.Muted, BorderThickness = default, BorderBrush = Brushes.Transparent,
            TextWrapping = TextWrapping.NoWrap, MaxLength = 80, Padding = new Thickness(10, 5.5, 20, 5.5),
            VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        SearchBox.Classes.Add("bar-search");
        SearchRing = Ui.FocusRing(new CornerRadius(9999));
        SearchFrame = new Panel
        {
            MinWidth = SearchMinWidth, MaxWidth = SearchMaxWidth, Margin = new Thickness(Ui.Spacing.Page, 0),
            VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Stretch,
            Children = { SearchBox, SearchRing }
        };
        SearchBox.PropertyChanged += (_, e) =>
        {
            if (e.Property == IsFocusedProperty) SearchRing.BorderBrush = SearchBox.IsFocused ? Ui.OnPrimary : Brushes.Transparent;
        };
        AutomationProperties.SetName(SearchBox, "Cari menu atau restoran");
        SearchGlyph = Glyph(SearchIcon, null, default);
        SearchButton = Plain("bar-search-button", SearchGlyph, () => search(SearchBox.Text ?? ""));
        SearchButton.Classes.Remove(Ui.OnPrimaryClass);
        SearchButton.Width = SearchButton.Height = 40; SearchButton.Padding = default; SearchButton.Margin = new Thickness(8, 0, 0, 0);
        SearchButton.CornerRadius = new CornerRadius(SearchButton.Width / 2); SearchButton.Focusable = false; SearchButton.VerticalAlignment = VerticalAlignment.Center;
        SearchGlyph[!Shape.StrokeProperty] = SearchButton[!TemplatedControl.ForegroundProperty];
        AutomationProperties.SetName(SearchButton, "Cari");
        ToolTip.SetTip(SearchButton, "Cari");
        SearchBox.InnerLeftContent = SearchButton;
        SearchBox.AddHandler(KeyDownEvent, (_, e) =>
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            search(SearchBox.Text ?? "");
        }, RoutingStrategies.Tunnel);

        // Cart
        CartGlyph = Glyph(CartIcon, null, default);
        CartBadgeText = new TextBlock { FontSize = 12, Foreground = Ui.Green, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        CartBadge = new Border
        {
            Child = CartBadgeText, Background = Ui.OnPrimary, CornerRadius = new CornerRadius(9999), MinWidth = 18, Height = 18,
            Padding = new Thickness(6, 0), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top,
            IsHitTestVisible = false, IsVisible = false
        };
        CartIndicator = new Border
        {
            Width = 20, Height = 2, CornerRadius = new CornerRadius(1), Background = Ui.OnPrimary, Margin = new Thickness(0, 0, 0, 2),
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom, IsHitTestVisible = false, IsVisible = false
        };
        var cartContent = new Grid { Width = 40, Height = 40, Children = { CartGlyph, CartIndicator, CartBadge } };
        CartButton = Plain("bar-cart", cartContent, cart);
        CartButton.Padding = default; CartButton.Width = CartButton.Height = 44;
        CartGlyph[!Shape.StrokeProperty] = CartButton[!TemplatedControl.ForegroundProperty];
        AutomationProperties.SetName(CartButton, "Keranjang");
        ToolTip.SetTip(CartButton, "Keranjang");

        // Profile
        var userGlyph = Glyph(UserIcon, null, default);
        ProfileButton = Plain("bar-profile", userGlyph, () => { });
        ProfileButton.Width = ProfileButton.Height = 44; ProfileButton.Padding = default;
        ProfileButton.CornerRadius = new CornerRadius(ProfileButton.Width / 2);
        ProfileButton.HorizontalContentAlignment = HorizontalAlignment.Center;
        userGlyph[!Shape.StrokeProperty] = ProfileButton[!TemplatedControl.ForegroundProperty];
        AutomationProperties.SetName(ProfileButton, "Akun");
        ToolTip.SetTip(ProfileButton, "Akun");

        // Profile menu
        NameText = Ui.Text("", 14, Ui.Muted, true);
        EmailText = Ui.Text("", 12, Ui.Muted);
        var header = Ui.Stack(6, NameText, EmailText);
        header.Margin = new Thickness(12, 8, 12, 8);
        ProfileMenu = new Popup
        {
            PlacementTarget = ProfileButton, Placement = PlacementMode.BottomEdgeAlignedRight,
            HorizontalOffset = 24, VerticalOffset = -8, IsLightDismissEnabled = true, ShouldUseOverlayLayer = true
        };
        OrdersItem = MenuItem("Pesanan saya", orders);
        SignOutItem = MenuItem("Keluar", signOut);
        var menuDivider = Ui.Divider(); menuDivider.Margin = new Thickness(0, 6);
        MenuCard = new Border
        {
            Width = 260, Padding = new Thickness(8), Background = Brushes.White, CornerRadius = new CornerRadius(16),
            BoxShadow = Ui.CardShadow, Margin = new Thickness(24, 16, 24, 32),
            Child = Ui.Stack(0, header, menuDivider, OrdersItem, SignOutItem)
        };
        AutomationProperties.SetName(MenuCard, "Menu akun");
        ProfileMenu.Child = MenuCard;
        MenuCard.AddHandler(KeyDownEvent, OnMenuKey, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        ProfileMenu.Opened += (_, _) => Avalonia.Threading.Dispatcher.UIThread.Post(() => OrdersItem.Focus(MenuFocus));
        ProfileMenu.Closed += (_, _) => { if (MenuCard.IsKeyboardFocusWithin) ProfileButton.Focus(MenuFocus); };
        ProfileButton.AddHandler(KeyDownEvent, (_, e) => keyboardOpen = e.Key is Key.Enter or Key.Space, RoutingStrategies.Tunnel, true);
        ProfileButton.AddHandler(KeyUpEvent, (_, e) => keyboardOpen = e.Key is Key.Enter or Key.Space, RoutingStrategies.Tunnel, true);
        ProfileButton.AddHandler(PointerPressedEvent, (_, _) => keyboardOpen = false, RoutingStrategies.Tunnel, true);
        ProfileButton.Click += (_, _) =>
        {
            MenuOpenedByKeyboard = keyboardOpen; keyboardOpen = false;
            ProfileMenu.IsOpen = !ProfileMenu.IsOpen;
        };

        // Layout
        var actions = Ui.Row(16, CartButton, ProfileButton);
        actions.VerticalAlignment = VerticalAlignment.Center;
        LogoButton.VerticalAlignment = VerticalAlignment.Center;
        var grid = Ui.Columns("Auto,*,Auto", LogoButton, SearchFrame, actions);
        Grid.SetColumn(ProfileMenu, 2); grid.Children.Add(ProfileMenu);
        Child = grid;
    }

    internal void Show(User? user, int cartCount, bool cartActive)
    {
        ProfileMenu.IsOpen = false;
        NameText.Text = user?.Name ?? ""; EmailText.Text = user?.Email ?? "";
        CartButton.Classes.Set("active", cartActive);
        CartIndicator.IsVisible = cartActive;
        SetCartCount(cartCount);
    }

    internal void SetCartCount(int count)
    {
        CartBadge.IsVisible = count > 0;
        CartBadgeText.Text = count > 99 ? "99+" : count.ToString();
        AutomationProperties.SetHelpText(CartButton, count > 0 ? $"{count} item" : "Kosong");
    }

    private bool keyboardOpen;
    internal bool MenuOpenedByKeyboard { get; private set; }
    private NavigationMethod MenuFocus => MenuOpenedByKeyboard ? NavigationMethod.Tab : NavigationMethod.Pointer;

    private void OnMenuKey(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Escape or Key.Down or Key.Up) MenuOpenedByKeyboard = true;
        if (e.Key == Key.Escape) { ProfileMenu.IsOpen = false; ProfileButton.Focus(NavigationMethod.Tab); e.Handled = true; }
        else if (e.Key is Key.Down or Key.Up)
        {
            (SignOutItem.IsFocused ? OrdersItem : SignOutItem).Focus(NavigationMethod.Directional);
            e.Handled = true;
        }
    }

    private Button MenuItem(string text, Action action)
    {
        var item = Plain("bar-menu-item", text, () => { ProfileMenu.IsOpen = false; action(); });
        item.HorizontalAlignment = HorizontalAlignment.Stretch;
        item.HorizontalContentAlignment = HorizontalAlignment.Left;
        item.Padding = new Thickness(12, 11); item.FontSize = 14;
        item.Classes.Remove(Ui.OnPrimaryClass); item.Classes.Add(Ui.FocusFillClass);
        return item;
    }

    private static Button Plain(string kind, object content, Action action)
    {
        var button = new Button
        {
            Theme = PlainButton, Content = content, CornerRadius = new CornerRadius(6), BorderThickness = new Thickness(2),
            HorizontalContentAlignment = HorizontalAlignment.Center, Cursor = new Cursor(StandardCursorType.Hand)
        };
        button.Classes.Add(kind); button.Classes.Add(Ui.OnPrimaryClass);
        button.Click += (_, _) => action();
        return button;
    }

    private static Path Glyph(Geometry data, IBrush? stroke, Thickness margin) => new()
    {
        Data = data, Width = 24, Height = 24, Stretch = Stretch.None, Stroke = stroke, StrokeThickness = 1.75,
        StrokeLineCap = PenLineCap.Round, StrokeJoin = PenLineJoin.Round, Margin = margin,
        HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false
    };

    // Styles
    private void AddStyles()
    {
        Styles.Add(State("bar-logo", x => x, Brushes.Transparent, null, Brushes.Transparent));
        Styles.Add(State("bar-search-button", x => x, Brushes.Transparent, Ui.Muted, Brushes.Transparent));
        Styles.Add(State("bar-search-button", x => x.Class(":pointerover"), Brushes.Transparent, Ui.Green));
        Styles.Add(State("bar-cart", x => x, Brushes.Transparent, Ui.OnPrimary, Brushes.Transparent));
        Styles.Add(State("bar-cart", x => x.Class(":pointerover"), Brushes.Transparent, Selection));
        Styles.Add(State("bar-profile", x => x, Ui.OnPrimary, Ui.Green, Brushes.Transparent));
        Styles.Add(State("bar-profile", x => x.Class(":pointerover"), Selection, Ui.Green));
        Styles.Add(State("bar-menu-item", x => x, Brushes.Transparent, Ui.Ink, Brushes.Transparent));
        Styles.Add(State("bar-menu-item", x => x.Class(":pointerover"), Selection, Ui.Green));
        foreach (var state in new Func<Selector, Selector>[] { x => x, x => x.Class(":pointerover"), x => x.Class(":focus"), x => x.Class(":focus-within") })
            Styles.Add(new Style(x => state(x.OfType<TextBox>().Class("bar-search")).Template().OfType<Border>().Name("PART_BorderElement"))
            {
                Setters =
                {
                    new Setter(Border.BackgroundProperty, Brushes.White), new Setter(Border.BorderBrushProperty, Brushes.Transparent),
                    new Setter(Border.BorderThicknessProperty, default(Thickness))
                }
            });
        Styles.Add(Ui.FocusStyles());
    }

    private static Style State(string kind, Func<Selector, Selector> state, IBrush? background, IBrush? foreground, IBrush? border = null)
    {
        var style = new Style(x => state(x.OfType<Button>().Class(kind)));
        if (background is not null) style.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, background));
        if (foreground is not null) style.Setters.Add(new Setter(TemplatedControl.ForegroundProperty, foreground));
        if (border is not null) style.Setters.Add(new Setter(TemplatedControl.BorderBrushProperty, border));
        return style;
    }
}
