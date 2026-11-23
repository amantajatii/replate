using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Replate.Domain.Entities;

namespace Replate.Desktop;

internal static class TopBarVerification
{
    private static readonly Color Primary = Color.Parse("#1F6B4F");
    private static readonly Color Ink = Color.Parse("#202522");
    private static readonly Color Muted = Color.Parse("#69736D");
    private static readonly Color Selection = Color.Parse("#DDEDE5");

    internal static async Task Run(MainWindow window)
    {
        var bar = window.TopBar;
        var session = window.Session;

        // Elements
        Ensure(window.CurrentPage is MarketplaceView && bar.IsEffectivelyVisible && window.GetVisualDescendants().Contains(bar), "Customer top bar is shown");
        Ensure(Math.Abs(bar.Bounds.Height - CustomerTopBar.BarHeight) < .5 && ColorOf(bar.Background) == Primary &&
            bar.BorderThickness == default && bar.BoxShadow.Count == 0, "Top bar has a fixed height and a primary fill with no bottom rule");
        Ensure(bar.LogoButton.Content == bar.Logo && bar.Logo.Source == Ui.LogoWhite && bar.Logo.Stretch == Stretch.Uniform &&
            Math.Abs(bar.Logo.Bounds.Height - CustomerTopBar.LogoHeight) < .5 && double.IsNaN(bar.Logo.Width) && Contrast(Colors.White, Primary) >= 4.5,
            $"White logo sits on the primary top bar ({bar.Logo.Bounds.Width:0.#}x{bar.Logo.Bounds.Height:0.#}, {Contrast(Colors.White, Primary):0.0}:1)");
        Ensure(!bar.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "replate."), "Text wordmark is removed from the top bar");
        Ensure(AutomationProperties.GetName(bar.LogoButton) == Ui.LogoName && ToolTip.GetTip(bar.LogoButton) as string == "Jelajah", "Logo button is named Replate");
        CheckRatio(bar.Logo, "top bar logo");
        Ensure(ColorOf(bar.SearchBox.Background) == Colors.White && bar.SearchBox.BorderThickness == default && bar.SearchBox.CornerRadius == new CornerRadius(9999) &&
            ColorOf(bar.SearchBox.Foreground) == Ink && ColorOf(bar.SearchBox.PlaceholderForeground) == Muted &&
            bar.SearchBox.InnerLeftContent == bar.SearchButton && ColorOf(bar.SearchGlyph.Stroke) == Muted && !bar.SearchButton.Focusable, "Search is a white pill with muted icon and placeholder and ink text");
        var searchBorder = bar.SearchBox.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_BorderElement");
        Ensure(searchBorder.BorderThickness == default && ColorOf(bar.SearchRing.BorderBrush) == Colors.Transparent, "Search has no dark border at rest");
        bar.SearchBox.Focus(); await Settle();
        Ensure(searchBorder.BorderThickness == default && ColorOf(bar.SearchRing.BorderBrush) == Colors.White && bar.SearchRing.BorderThickness == new Thickness(Ui.FocusRingWidth) &&
            Contrast(Colors.White, Primary) >= 3, "Focused search shows a thin white ring, not a black border");
        bar.LogoButton.Focus(NavigationMethod.Pointer); await Settle();
        Ensure(ColorOf(bar.SearchRing.BorderBrush) == Colors.Transparent, "Search ring hides when focus leaves");
        Ensure(bar.SearchBox.PlaceholderText == "Mulai cari menu atau restoran" && bar.SearchBox.IsEffectivelyVisible, "Search field shows its placeholder");
        Ensure(AutomationProperties.GetName(bar.CartButton) == "Keranjang" && ToolTip.GetTip(bar.CartButton) as string == "Keranjang", "Cart icon is named Keranjang");
        Ensure(AutomationProperties.GetName(bar.ProfileButton) == "Akun" && ToolTip.GetTip(bar.ProfileButton) as string == "Akun", "Profile circle is named Akun");
        Ensure(new Control[] { bar.LogoButton, bar.SearchBox, bar.LocationChip, bar.CartButton, bar.ProfileButton }.All(c => c.Focusable && c.IsEnabled && KeyboardNavigation.GetIsTabStop(c)),
            "Every top bar control is reachable with Tab");
        Ensure(!window.GetVisualDescendants().OfType<Button>().Any(b => b.Content is string text && (text is "Jelajah" or "Pesanan saya" or "Keluar" || text.StartsWith("Keranjang"))),
            "Old Jelajah, Pesanan saya, Keranjang and Keluar tabs are removed");
        Ensure(!window.CurrentPage!.GetVisualDescendants().OfType<TextBox>().Any() &&
            !window.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Cari makanan atau restoran"), "Old marketplace search field and label are removed");

        // Icons
        Ensure(bar.CartGlyph.StrokeThickness == 1.75 && bar.CartGlyph.StrokeLineCap == PenLineCap.Round && ColorOf(bar.CartGlyph.Stroke) == Colors.White &&
            Contrast(Colors.White, Primary) >= 3, "Cart icon is a white line icon");
        await Pseudo(bar.CartButton, ":pointerover", true);
        Ensure(ColorOf(bar.CartGlyph.Stroke) == Selection && ColorOf(bar.CartButton.Background) == Colors.Transparent && Contrast(Selection, Primary) >= 3,
            $"Cart hover tints to selection with no background box ({Contrast(Selection, Primary):0.0}:1)");
        await Pseudo(bar.CartButton, ":pointerover", false);
        Ensure(ColorOf(bar.ProfileButton.Background) == Colors.White && ColorOf(bar.ProfileButton.Foreground) == Primary && Contrast(Primary, Colors.White) >= 3,
            "Profile circle is white with a primary icon");
        await Pseudo(bar.ProfileButton, ":pointerover", true);
        Ensure(ColorOf(bar.ProfileButton.Background) == Selection && Contrast(Primary, Selection) >= 3, "Profile hover darkens to selection");
        await Pseudo(bar.ProfileButton, ":pointerover", false);

        // Focus
        foreach (var button in new[] { bar.LogoButton, bar.LocationChip, bar.CartButton, bar.ProfileButton })
        {
            var ring = FocusRing(button);
            Ensure(button.FocusAdorner == Ui.NoFocusAdorner && ColorOf(ring.BorderBrush) == Colors.Transparent, $"{AutomationProperties.GetName(button)}: no default focus adorner and no ring at rest");
            await Pseudo(button, ":focus-visible", true);
            Ensure(ColorOf(ring.BorderBrush) == Colors.White && ring.BorderThickness == new Thickness(Ui.FocusRingWidth) && ColorOf(button.BorderBrush) == Colors.Transparent &&
                Rect(ring, button).X <= -Ui.FocusRingOffset, $"{AutomationProperties.GetName(button)}: keyboard focus shows a 2 px white ring with an offset (ring {ColorOf(ring.BorderBrush)}, {ring.BorderThickness}, border {ColorOf(button.BorderBrush)}, x {Rect(ring, button).X:0.#})");
            await Pseudo(button, ":focus-visible", false);
        }

        // Badge
        Ensure(session.Cart.Items.Count == 0 && !bar.CartBadge.IsVisible, "Badge hidden when the cart is empty");
        var listing = session.Listings.First(l => l.IsAvailable(DateTime.UtcNow) && l.Menu.IsActive && l.Quantity >= 2);
        window.Marketplace.Add(listing); await Settle();
        Ensure(bar.CartBadge.IsVisible && bar.CartBadgeText.Text == "1" && ColorOf(bar.CartBadge.Background) == Colors.White &&
            ColorOf(bar.CartBadgeText.Foreground) == Primary && bar.CartBadge.CornerRadius == new CornerRadius(9999), "Badge is a white circle with a primary number");
        window.Marketplace.Add(listing); await Settle();
        Ensure(bar.CartBadgeText.Text == "2", "Badge follows the item count");
        Ensure(Contains(Rect(bar.CartBadge, bar), Rect(bar.CartButton, bar)), "Badge stays inside the cart icon");

        // Active cart
        Click(bar.CartButton); await Settle();
        Ensure(window.CurrentPage is CartView && bar.CartIndicator.IsVisible && ColorOf(bar.CartIndicator.Background) == Colors.White &&
            Rect(bar.CartIndicator, bar.CartButton).Y >= Rect(bar.CartGlyph, bar.CartButton).Bottom - .5 && Contains(Rect(bar.CartIndicator, bar), Rect(bar.CartButton, bar)),
            "Cart icon opens Keranjang and shows a thin white line under the icon");
        window.Run(() => session.ChangeQuantity(listing.Id, 0), window.ShowCart); await Settle();
        Ensure(!bar.CartBadge.IsVisible, "Badge hides again at 0");
        Click(bar.LogoButton); await Settle();
        Ensure(window.CurrentPage is MarketplaceView && !bar.CartIndicator.IsVisible && ColorOf(bar.CartGlyph.Stroke) == Colors.White,
            "Logo opens Jelajah and the cart line hides");

        // Search
        bar.SearchBox.Text = "croissant"; await Settle();
        Ensure(window.CurrentPage is MarketplaceView { VisibleCount: 10 }, "Typing does not filter Jelajah directly");
        window.ShowCart(); await Settle();
        Ensure(bar.SearchBox.Text == "", "Leaving Jelajah clears the search");
        bar.SearchBox.Focus();
        bar.SearchBox.Text = "croissant"; await Settle();
        Ensure(window.CurrentPage is CartView && bar.SearchBox.IsFocused, "Typing on Keranjang stays on Keranjang");
        bar.SearchBox.Text = ""; await Settle();
        window.ShowMarketplace(); await Settle();

        // Profile menu
        Ensure(!bar.ProfileMenu.IsOpen && bar.ProfileMenu.IsLightDismissEnabled && bar.ProfileMenu.PlacementTarget == bar.ProfileButton,
            "Profile menu is a light-dismiss popup anchored to the circle");
        Click(bar.ProfileButton); await Settle();
        Ensure(bar.ProfileMenu.IsOpen && bar.MenuCard.IsEffectivelyVisible, "Profile circle opens the menu");
        Ensure(bar.NameText.Text == session.CurrentUser!.Name && bar.EmailText.Text == session.CurrentUser.Email &&
            ColorOf(bar.NameText.Foreground) == Color.Parse("#69736D") && ColorOf(bar.EmailText.Foreground) == Color.Parse("#69736D") &&
            bar.NameText.FindAncestorOfType<Button>() is null && bar.EmailText.FindAncestorOfType<Button>() is null,
            "Menu shows muted, non-clickable name and email");
        Ensure(bar.MenuCard.GetVisualDescendants().OfType<Button>().Select(b => b.Content as string).SequenceEqual(["Pesanan saya", "Keluar"]),
            "Menu holds Pesanan saya and Keluar");
        Ensure(bar.MenuCard.CornerRadius == new CornerRadius(16) && bar.MenuCard.BoxShadow.ToString() == BoxShadows.Parse("0 8 24 0 #1F202522").ToString() &&
            bar.MenuCard.BorderThickness == default && ColorOf(bar.MenuCard.Background) == Colors.White, "Menu is a white card with the card shadow and no border");
        Ensure(bar.OrdersItem.IsFocused && !FocusVisible(bar.OrdersItem) && ColorOf(bar.OrdersItem.Background) == Colors.Transparent,
            "Opening the menu with the mouse focuses Pesanan saya without a visible focus");
        foreach (var item in new[] { bar.OrdersItem, bar.SignOutItem })
        {
            Ensure(item.FocusAdorner == Ui.NoFocusAdorner && !HasAdorner(item), $"{item.Content}: no default focus adorner");
            Ensure(FocusStyled(item), $"{item.Content}: has a :focus-visible style");
        }
        Key(bar.OrdersItem, Avalonia.Input.Key.Down); await Settle();
        Ensure(bar.SignOutItem.IsFocused && FocusVisible(bar.SignOutItem) && ColorOf(bar.SignOutItem.Background) == Selection &&
            ColorOf(FocusRing(bar.SignOutItem).BorderBrush) == Colors.Transparent && !HasAdorner(bar.SignOutItem),
            $"Down arrow moves to Keluar with a selection fill and no black box (focused {bar.SignOutItem.IsFocused}, visible {FocusVisible(bar.SignOutItem)}, bg {ColorOf(bar.SignOutItem.Background)}, ring {ColorOf(FocusRing(bar.SignOutItem).BorderBrush)}, adorner {HasAdorner(bar.SignOutItem)})");
        Key(bar.SignOutItem, Avalonia.Input.Key.Escape); await Settle();
        Ensure(!bar.ProfileMenu.IsOpen && bar.ProfileButton.IsFocused, "Esc closes the menu and returns focus to the circle");
        Key(bar.ProfileButton, Avalonia.Input.Key.Enter); await Settle();
        Ensure(bar.ProfileMenu.IsOpen && bar.MenuOpenedByKeyboard && bar.OrdersItem.IsFocused && FocusVisible(bar.OrdersItem) &&
            ColorOf(bar.OrdersItem.Background) == Selection && !HasAdorner(bar.OrdersItem), "Enter opens the menu with a visible focus on Pesanan saya");
        Key(bar.OrdersItem, Avalonia.Input.Key.Escape); await Settle();
        bar.ProfileButton.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Avalonia.Input.Key.Space, Source = bar.ProfileButton });
        bar.ProfileButton.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyUpEvent, Key = Avalonia.Input.Key.Space, Source = bar.ProfileButton }); await Settle();
        Ensure(bar.ProfileMenu.IsOpen && FocusVisible(bar.OrdersItem), "Space opens the menu with a visible focus");
        Click(bar.OrdersItem); await Settle();
        Ensure(window.CurrentPage is OrdersView && !bar.ProfileMenu.IsOpen, "Pesanan saya opens the orders page and closes the menu");
        Click(bar.LogoButton); await Settle();

        // Layout
        var (width, height) = (window.Width, window.Height);
        window.Width = 1040; window.Height = 720; await Settle();
        CheckLayout(bar, "window at 1040x720");
        window.Width = width; window.Height = height; await Settle();
        foreach (var size in new[] { 1040d, 1920d })
        {
            var sample = new CustomerTopBar(() => { }, () => { }, () => { }, () => { }, _ => { }, window.Session, () => Task.CompletedTask);
            sample.SetCartCount(120);
            var frame = new Window { Width = 1040, Height = 720, Content = new Canvas { Children = { new Border { Child = sample, Width = size } } } };
            try { frame.Show(); await Settle(); CheckLayout(sample, $"{size:0} wide"); }
            finally { frame.Close(); }
        }
    }

    internal static async Task SignOut(MainWindow window)
    {
        var bar = window.TopBar;
        Click(bar.ProfileButton); await Settle();
        Ensure(bar.ProfileMenu.IsOpen, "Profile menu opens before Keluar");
        Click(bar.SignOutItem); await Settle();
        var confirm = window.SignOutConfirmation;
        Ensure(confirm is not null && window.Session.IsSignedIn && !bar.ProfileMenu.IsOpen, "Keluar in the profile menu asks for confirmation first");
        Click(confirm!.ConfirmButton); await Settle();
        Ensure(window.SignOutConfirmation is null && window.CurrentPage is SignInView && !window.Session.IsSignedIn && !bar.ProfileMenu.IsOpen && bar.SearchBox.Text == "" &&
            !window.GetVisualDescendants().Contains(bar), "Keluar in the profile menu signs out and returns to sign-in");
    }

    private static void CheckLayout(CustomerTopBar bar, string label)
    {
        var parts = new (string Name, Control Control)[] { ("logo", bar.LogoButton), ("search", bar.SearchBox),
            ("location", bar.LocationChip), ("cart", bar.CartButton), ("profile", bar.ProfileButton) };
        var area = new Rect(bar.Bounds.Size);
        foreach (var (name, control) in parts)
            Ensure(control.IsEffectivelyVisible && Contains(Rect(control, bar), area), $"{label}: {name} sits inside the top bar");
        for (var i = 0; i < parts.Length; i++)
            for (var j = i + 1; j < parts.Length; j++)
                Ensure(!Rect(parts[i].Control, bar).Intersects(Rect(parts[j].Control, bar)), $"{label}: {parts[i].Name} and {parts[j].Name} overlap");
        var search = Rect(bar.SearchBox, bar);
        Ensure(search.Width >= CustomerTopBar.SearchMinWidth - .5 && search.Width <= CustomerTopBar.SearchMaxWidth + .5, $"{label}: search is {search.Width:0} wide");
        var left = search.X - Rect(bar.LogoButton, bar).Right;
        var right = Rect(bar.LocationChip, bar).X - search.Right;
        Ensure(left >= 31.5 && right >= 31.5 && Math.Abs(left - right) < 1, $"{label}: search is centered between logo and icons ({left:0}, {right:0})");
        Ensure(Contains(Rect(bar.CartBadge, bar), Rect(bar.CartButton, bar)), $"{label}: badge stays inside the cart icon");
        Ensure(Math.Abs(bar.Bounds.Height - CustomerTopBar.BarHeight) < .5, $"{label}: top bar stays {CustomerTopBar.BarHeight:0} high ({bar.Bounds.Height:0.#})");
        CheckRatio(bar.Logo, $"{label}: logo");
    }

    internal static void CheckRatio(Image logo, string label)
    {
        var pixels = ((Avalonia.Media.Imaging.Bitmap)logo.Source!).PixelSize;
        var expected = (double)pixels.Width / pixels.Height;
        var actual = logo.Bounds.Width / logo.Bounds.Height;
        Ensure(logo.Bounds.Height > 0 && Math.Abs(actual / expected - 1) <= .01, $"{label} keeps the bitmap ratio ({actual:0.###}, bitmap {expected:0.###})");
    }

    internal static double Contrast(Color a, Color b)
    {
        static double Channel(byte c) { var v = c / 255d; return v <= .03928 ? v / 12.92 : Math.Pow((v + .055) / 1.055, 2.4); }
        static double Luminance(Color c) => .2126 * Channel(c.R) + .7152 * Channel(c.G) + .0722 * Channel(c.B);
        var (light, dark) = (Math.Max(Luminance(a), Luminance(b)), Math.Min(Luminance(a), Luminance(b)));
        return (light + .05) / (dark + .05);
    }

    internal static Border FocusRing(Control control) =>
        control.GetVisualDescendants().OfType<Border>().Single(b => b.Name == Ui.FocusRingPart && b.TemplatedParent == control);

    internal static bool FocusVisible(Control control) => control.Classes.Contains(":focus-visible");

    internal static bool HasAdorner(Visual control) =>
        AdornerLayer.GetAdornerLayer(control)?.Children.Any(a => AdornerLayer.GetAdornedElement(a) == control && a.GetVisualDescendants().OfType<Border>().Any()) == true;

    private static IEnumerable<IStyle> FindStyles(StyledElement element) => element.GetLogicalAncestors().OfType<StyledElement>().SelectMany(e => e.Styles);

    private static bool FocusStyled(Control control) =>
        FindStyles(control).SelectMany(Flatten).OfType<Style>().Any(s => s.Selector?.ToString().Contains(":focus-visible") == true && s.Selector.ToString().Contains(Ui.FocusFillClass));

    private static IEnumerable<IStyle> Flatten(IStyle style) => style is Styles group ? group.SelectMany(Flatten) : [style];

    private static Rect Rect(Visual visual, Visual root) =>
        new(visual.TranslatePoint(default, root) ?? throw new InvalidOperationException("Element is outside the top bar"), visual.Bounds.Size);

    private static bool Contains(Rect inner, Rect outer) =>
        inner.X >= outer.X - .5 && inner.Y >= outer.Y - .5 && inner.Right <= outer.Right + .5 && inner.Bottom <= outer.Bottom + .5;

    private static Color ColorOf(IBrush? brush) => brush is ISolidColorBrush solid ? solid.Color : Colors.Transparent;

    private static async Task Pseudo(StyledElement element, string pseudo, bool on)
    { ((IPseudoClasses)element.Classes).Set(pseudo, on); await Task.Delay(100); }

    private static void Key(Control target, Key key) =>
        target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key, Source = target });

    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static async Task Settle()
    { await Task.Delay(250); await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render); }

    private static void Ensure(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
