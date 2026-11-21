using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Replate.Domain.Entities;

namespace Replate.Desktop;

internal static class CartVerification
{
    private static readonly Color Primary = Color.Parse("#245B43");
    private static readonly Color Divider = Color.Parse("#DDE3DA");
    private static readonly Color Muted = Color.Parse("#59675D");
    private static readonly Color Selection = Color.Parse("#E0E9DF");

    internal static async Task Run(MainWindow window)
    {
        var session = window.Session;
        var bar = window.TopBar;
        var saved = session.Cart.Items.Select(i => (i.Listing, i.Quantity)).ToList();
        var (width, height) = (window.Width, window.Height);
        Clear(session);
        var now = DateTime.UtcNow;
        var restaurantId = session.Listings.Where(l => l.IsAvailable(now) && l.Menu.IsActive && l.Quantity >= 2)
            .GroupBy(l => l.Menu.RestaurantId).OrderByDescending(g => g.Count()).First().Key;
        var picks = session.Listings.Where(l => l.Menu.RestaurantId == restaurantId && l.IsAvailable(now) && l.Menu.IsActive && l.Quantity >= 2).Take(3).ToList();
        Ensure(picks.Count >= 2, "Two available listings from one restaurant");
        var (first, second) = (picks[0], picks[1]);
        var restaurant = session.FindRestaurant(restaurantId)!;

        // Empty
        window.ShowCart(); await Settle();
        var view = Page(window);
        Ensure(view.EmptyState is { IsEffectivelyVisible: true } && view.Summary is null && view.CheckoutButton is null && view.Lines.Count == 0,
            "An empty cart shows the empty state without a summary card");
        var texts = Texts(view.EmptyState!);
        Ensure(texts.Contains(CartView.EmptyTitle) && texts.Contains(CartView.EmptyMessage) && view.EmptyButton?.Content as string == CartView.EmptyAction &&
            view.EmptyButton.Classes.Contains(Ui.PrimaryClass), "Empty state title, message and Jelajahi menu button");
        Ensure(view.EmptyIcon is { } icon && ColorOf(icon.Stroke) == Primary && icon.Fill is null && icon.Bounds.Width >= 64,
            "Empty state shows a large primary line cart icon");
        Ensure(view.Title.Text == CartView.TitleText && !view.Subtitle.IsVisible && view.BackLink.IsEffectivelyVisible, "Empty cart keeps the title and back link, no subtitle");
        var emptyBox = Rect(view.EmptyState!, window); var bodyBox = Rect(view.Body, window);
        Ensure(Math.Abs(emptyBox.Center.X - bodyBox.Center.X) <= 1 && emptyBox.Y > Rect(view.Header, window).Bottom, "The empty state is centered under the header");
        Click(view.EmptyButton!); await Settle();
        Ensure(window.CurrentPage is MarketplaceView, "Jelajahi menu opens Jelajah");

        // Header
        session.AddToCart(first, 1); session.AddToCart(second, 2);
        window.ShowCart(); await Settle();
        view = Page(window);
        Ensure(view.Title.Text == CartView.TitleText && view.Title.FontSize == MarketplaceView.TitleSize && view.Title.FontWeight >= FontWeight.Bold &&
            view.Subtitle.IsVisible && view.Subtitle.Text == $"Pesanan dari {restaurant.Name}" && view.Subtitle.FontSize < view.Title.FontSize &&
            ColorOf(view.Subtitle.Foreground) == Muted, "Keranjang title at the Jelajahi Menu size with the muted restaurant line");
        Ensure(view.BackLink.Classes.Contains(CollectionView.BackLinkClass) && AutomationProperties.GetName(view.BackLink) == CollectionView.BackLabel &&
            ColorOf(view.BackLink.Foreground) == Primary && view.BackGlyph.StrokeThickness == 1.75, "Kembali ke Jelajah uses the collection back link");
        var link = Rect(view.BackLink, window); var header = Rect(view.Header, window); var columns = Rect(view.Columns!, window);
        Ensure(Math.Abs(header.Y - link.Bottom - Ui.Spacing.Related) <= 1 && Math.Abs(columns.Y - header.Bottom - Ui.Spacing.Related) <= 1,
            "Back link, header and content are 16 apart");

        // Items
        Ensure(view.Lines.Count == 2 && view.ItemsPanel!.Children.Count == 2, "One card per cart item");
        foreach (var line in view.Lines)
        {
            var item = line.Item; var menu = item.Listing.Menu;
            Ensure(line.NameText.Text == menu.Name && line.NameText.FontWeight >= FontWeight.SemiBold && line.MetaText.Text == $"{menu.Restaurant?.Name} · {menu.Category}" &&
                line.PickupText.Text!.StartsWith("Pengambilan ") && line.PriceText.Text == $"{DemoSession.Rupiah(item.UnitPrice)} / porsi" &&
                line.Stepper.QuantityText.Text == item.Quantity.ToString() && line.SubtotalText.Text == DemoSession.Rupiah(item.CalculateSubtotal()) &&
                line.SubtotalText.FontWeight >= FontWeight.Bold, $"{menu.Name}: name, restaurant and category, pickup, unit price, quantity and subtotal");
            Ensure(line.Card.CornerRadius == new CornerRadius(16) && line.Card.BorderThickness == default && ColorOf(line.Card.Background) == Colors.White &&
                line.Card.BoxShadow.ToString() == MarketplaceView.CardShadow.ToString(), $"{menu.Name}: white card with the card radius and shadow, no border");
            Ensure(AutomationProperties.GetName(line.RemoveButton) == $"Hapus {menu.Name}" && line.RemoveButton.Content == line.RemoveGlyph &&
                line.RemoveGlyph.StrokeThickness == 1.75 && line.RemoveGlyph.Fill is null && ColorOf(line.RemoveButton.Background) == Colors.Transparent &&
                line.RemoveButton.Classes.Contains(Ui.IconClass) && line.RemoveButton.IsEnabled, $"{menu.Name}: trash icon button named Hapus, no box");
            Ensure(line.Stepper.MinusButton.CornerRadius == new CornerRadius(QuantityStepper.ButtonSize / 2) && line.Stepper.MinusButton.Width == QuantityStepper.ButtonSize &&
                line.Stepper.PlusButton.Content is PathIcon && AutomationProperties.GetName(line.Stepper.PlusButton) == $"Tambah jumlah {menu.Name}",
                $"{menu.Name}: the shared round stepper");
        }
        var cards = view.Lines.Select(l => Rect(l.Card, window)).ToList();
        Ensure(Math.Abs(cards[1].Y - cards[0].Bottom - Ui.Spacing.Related) <= 1, "Item cards are 16 apart");

        // Summary
        var summary = view.Summary!;
        Ensure(summary.Width == CartView.SummaryWidth && summary.CornerRadius == new CornerRadius(16) && ColorOf(summary.Background) == Colors.White &&
            summary.BoxShadow.ToString() == MarketplaceView.CardShadow.ToString() && Texts(summary).Contains(CartView.SummaryTitle) && Texts(summary).Contains(CartView.PickupNote),
            "Ringkasan pesanan card with title and pickup note");
        CheckTotals(view, session, bar);
        Ensure(view.RestaurantText!.Text == restaurant.Name, "Summary names the restaurant");
        var checkout = view.CheckoutButton!;
        Ensure(checkout.Content as string == CartView.CheckoutLabel && checkout.IsEnabled && checkout.Classes.Contains(Ui.PrimaryClass) &&
            !checkout.Classes.Contains(Ui.CompactClass) && Math.Abs(checkout.Bounds.Width - (summary.Bounds.Width - 2 * CartView.SummaryPadding)) <= 1 &&
            !view.CheckoutReason!.IsVisible, "Pesan sekarang is a full-width primary button, enabled without a reason");
        Ensure(Math.Abs(Rect(summary, window).Y - cards[0].Y) <= 1, "The summary top lines up with the first item card");

        // Quantity
        var line0 = view.Lines[0];
        Ensure(line0.Item.Quantity == 1 && !line0.Stepper.MinusButton.IsEnabled && line0.Stepper.PlusButton.IsEnabled &&
            ColorOf(line0.Stepper.MinusButton.Background) == Divider && ColorOf(line0.Stepper.PlusButton.Background) == Primary, "Minus is disabled at 1, plus is primary");
        Click(line0.Stepper.PlusButton); await Settle();
        Ensure(window.CurrentPage == view && line0.Item.Quantity == 2 && line0.Stepper.QuantityText.Text == "2" &&
            line0.SubtotalText.Text == DemoSession.Rupiah(first.RescuePrice * 2) && line0.Stepper.MinusButton.IsEnabled, "Plus raises the quantity and subtotal in place");
        await UiVerification.HoverShape(window, line0.Stepper.MinusButton, "Keranjang minus");
        await UiVerification.HoverShape(window, line0.Stepper.PlusButton, "Keranjang plus");
        CheckTotals(view, session, bar);
        Click(line0.Stepper.MinusButton); await Settle();
        Ensure(line0.Item.Quantity == 1 && line0.SubtotalText.Text == DemoSession.Rupiah(first.RescuePrice), "Minus lowers the quantity and subtotal");
        CheckTotals(view, session, bar);
        while (line0.Stepper.PlusButton.IsEnabled) { Click(line0.Stepper.PlusButton); await Settle(); }
        Ensure(line0.Item.Quantity == first.Quantity && !line0.Stepper.PlusButton.IsEnabled && ColorOf(line0.Stepper.PlusButton.Background) == Divider &&
            ColorOf(line0.Stepper.PlusButton.Foreground) == Muted, "Plus is disabled at the remaining stock");
        CheckTotals(view, session, bar);
        session.ChangeQuantity(first.Id, 1); window.ShowCart(); await Settle();
        view = Page(window);

        // Remove styles
        var remove = view.Lines[1].RemoveButton;
        await UiVerification.HoverShape(window, remove, "Keranjang Hapus");
        Ensure(remove.FocusAdorner == Ui.NoFocusAdorner && ColorOf(remove.Foreground) == Muted, "Trash icon rests muted with the shared focus style");
        await Pseudo(remove, ":pointerover", true);
        Ensure(ColorOf(remove.Foreground) == Primary && ColorOf(view.Lines[1].RemoveGlyph.Stroke) == Primary && ColorOf(Presenter(remove).Background) == Selection,
            "Trash icon turns primary on hover inside a selection circle");
        await Pseudo(remove, ":pointerover", false);
        var ring = TopBarVerification.FocusRing(remove);
        await Pseudo(remove, ":focus-visible", true);
        Ensure(ColorOf(ring.BorderBrush) == Primary, "Trash icon shows the focus ring on :focus-visible");
        await Pseudo(remove, ":focus-visible", false);
        Ensure(ColorOf(ring.BorderBrush) == Colors.Transparent || !ring.IsVisible || ring.BorderBrush is null, "No ring without keyboard focus");

        // Remove
        Click(remove); await Settle();
        view = Page(window);
        Ensure(view.Lines.Count == 1 && view.Lines[0].Item.ListingId == first.Id && session.Cart.Items.Count == 1, "Hapus removes the item");
        CheckTotals(view, session, bar);

        // Unavailable
        first.Menu.Deactivate();
        window.ShowCart(); await Settle();
        view = Page(window);
        var off = view.Lines[0];
        Ensure(off.PriceText.Text == CartView.UnavailableText && !off.Stepper.MinusButton.IsEnabled && !off.Stepper.PlusButton.IsEnabled && off.RemoveButton.IsEnabled,
            "An unavailable item keeps its card with the message, a disabled stepper and an active Hapus");
        Ensure(!view.CheckoutButton!.IsEnabled && view.CheckoutReason!.IsVisible && view.CheckoutReason.Text == CartView.UnavailableReason,
            "An unavailable item disables Pesan sekarang and gives the reason");
        first.Menu.Activate();
        var stock = first.Quantity;
        session.ChangeQuantity(first.Id, 2); first.UpdateStock(1);
        window.ShowCart(); await Settle();
        view = Page(window);
        Ensure(!view.CheckoutButton!.IsEnabled && view.CheckoutReason!.Text == CartView.InvalidReason && !view.Lines[0].Stepper.PlusButton.IsEnabled,
            "A cart above the stock disables Pesan sekarang with a short reason");
        first.UpdateStock(stock); session.ChangeQuantity(first.Id, 1);

        // Layout
        foreach (var listing in picks.Skip(1)) session.AddToCart(listing, 1);
        foreach (var (w, h) in new[] { (1040d, 720d), (1920d, 1080d) })
        {
            window.Width = w; window.Height = h;
            window.ShowMarketplace(); await Settle();
            var market = Rect(window.Marketplace.Body, window);
            window.ShowCart(); await Settle();
            view = Page(window);
            CheckLayout(window, view, market, $"{w:0}x{h:0}");
        }
        Ensure(!Texts(window).Any(t => t.Contains('—')), "No em dash on Keranjang");

        // Restore
        window.Width = width; window.Height = height;
        Clear(session);
        foreach (var (listing, quantity) in saved) session.AddToCart(listing, quantity);
        window.ShowMarketplace(); await Settle();
        Console.WriteLine("All Replate cart checks passed (header, item cards, summary, stepper and stock limit, Hapus, unavailable, empty state, 1040x720 and 1920x1080).");
    }

    // Checks
    private static void CheckTotals(CartView view, DemoSession session, CustomerTopBar bar)
    {
        var portions = session.Cart.Items.Sum(i => i.Quantity);
        Ensure(view.CountText!.Text == $"{portions} porsi" && view.TotalText!.Text == DemoSession.Rupiah(session.Cart.Items.Sum(i => i.Quantity * i.UnitPrice)) &&
            view.TotalText.FontWeight >= FontWeight.Bold && view.TotalText.FontSize > view.CountText.FontSize,
            $"Summary shows {portions} porsi and the total ({view.CountText.Text}, {view.TotalText!.Text})");
        Ensure(bar.CartBadge.IsVisible == portions > 0 && (portions == 0 || bar.CartBadgeText.Text == portions.ToString()), "The cart badge follows the cart");
    }

    private static void CheckLayout(MainWindow window, CartView view, Rect market, string size)
    {
        var body = Rect(view.Body, window);
        var summary = Rect(view.Summary!, window);
        var scroll = view.ListScroll!;
        Ensure(Math.Abs(body.X - market.X) <= 1 && Math.Abs(body.Right - market.Right) <= 1, $"{size}: Keranjang edges line up with Jelajah ({body} vs {market})");
        Ensure(Math.Abs(summary.Right - body.Right) <= 1 && summary.Bottom <= window.Bounds.Height + .5 && summary.Width == CartView.SummaryWidth,
            $"{size}: the summary sits at the right edge, fully visible ({summary})");
        Ensure(scroll.Extent.Width <= scroll.Viewport.Width + .5 && window.GetVisualDescendants().OfType<ScrollViewer>().Where(s => s.IsEffectivelyVisible)
            .All(s => s.Extent.Width <= s.Viewport.Width + .5), $"{size}: no horizontal scrolling");
        foreach (var line in view.Lines)
        {
            var card = Rect(line.Card, window);
            if (card.Bottom > Rect(scroll, window).Bottom) continue;
            Ensure(Math.Abs(card.X - body.X) <= 1 && card.Right <= summary.X - Ui.Spacing.Section + 1, $"{size}: {line.NameText.Text} card fills the left column without touching the summary");
            var parts = new Control[] { line.Photo, line.NameText, line.MetaText, line.PickupText, line.PriceText, line.Stepper, line.SubtotalText, line.RemoveButton }
                .Select(c => (c, Rect(c, window))).ToList();
            Ensure(parts.All(p => Inside(p.Item2, card)), $"{size}: {line.NameText.Text} content stays inside its card");
            var columns = new[] { Rect(line.Photo, window), Union(line), Rect(line.Stepper, window), Rect(line.SubtotalText, window), Rect(line.RemoveButton, window) };
            for (var i = 1; i < columns.Length; i++)
                Ensure(columns[i].X >= columns[i - 1].Right - .5, $"{size}: {line.NameText.Text} columns do not overlap ({columns[i - 1]} and {columns[i]})");
        }
        Ensure(Rect(view.Header, window).Bottom <= Rect(view.Columns!, window).Y + .5 && Rect(view.BackLink, window).Bottom <= Rect(view.Header, window).Y + .5,
            $"{size}: header parts do not overlap");
        Rect Union(CartView.Line line) => new[] { line.NameText, line.MetaText, line.PickupText, line.PriceText }.Select(t => Rect(t, window)).Aggregate((a, b) => a.Union(b));
    }

    // Helpers
    private static void Clear(DemoSession session)
    {
        foreach (var item in session.Cart.Items.ToList()) session.ChangeQuantity(item.ListingId, 0);
    }

    private static CartView Page(MainWindow window) => window.CurrentPage as CartView ?? throw new InvalidOperationException("Keranjang is open");

    private static List<string> Texts(Visual root) => root.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").ToList();

    private static bool Inside(Rect inner, Rect outer) =>
        inner.X >= outer.X - .5 && inner.Y >= outer.Y - .5 && inner.Right <= outer.Right + .5 && inner.Bottom <= outer.Bottom + .5;

    private static Rect Rect(Visual visual, Visual root) =>
        new(visual.TranslatePoint(default, root) ?? throw new InvalidOperationException("Element is outside the window"), visual.Bounds.Size);

    private static ContentPresenter Presenter(Button button) =>
        button.GetVisualDescendants().OfType<ContentPresenter>().First(p => p.Name == "PART_ContentPresenter");

    private static Color ColorOf(IBrush? brush) => brush is ISolidColorBrush solid ? solid.Color : Colors.Transparent;

    private static async Task Pseudo(StyledElement element, string pseudo, bool on)
    { ((IPseudoClasses)element.Classes).Set(pseudo, on); await Task.Delay(100); }

    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static async Task Settle()
    { await Task.Delay(250); await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render); }

    private static void Ensure(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
