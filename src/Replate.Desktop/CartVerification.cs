using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Replate.Domain.Entities;

namespace Replate.Desktop;

internal static class CartVerification
{
    private static readonly Color Primary = Color.Parse("#1F6B4F");
    private static readonly Color Divider = Color.Parse("#D7E4DF");
    private static readonly Color Muted = Color.Parse("#69736D");
    private static readonly Color Ink = Color.Parse("#202522");
    private static readonly Color Selection = Color.Parse("#DDEDE5");
    private const string GroupNote = "Ambil di restoran ini";
    private const string PaymentNote = "Reservasi tidak melibatkan pembayaran.";

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
        Ensure(view.EmptyState is { IsEffectivelyVisible: true } && view.ListScroll is null && view.Groups.Count == 0 && view.Lines.Count == 0,
            "An empty cart shows the empty state without groups or order buttons");
        var texts = Texts(view.EmptyState!);
        Ensure(texts.Contains(CartView.EmptyTitle) && texts.Contains(CartView.EmptyMessage) && view.EmptyButton?.Content as string == CartView.EmptyAction &&
            view.EmptyButton.Classes.Contains(Ui.PrimaryClass), "Empty state title, message and Jelajahi menu button");
        foreach (var (w, h) in new[] { (1040d, 720d), (1920d, 1080d) })
        {
            window.Width = w; window.Height = h; await Settle();
            await UiVerification.EmptyIcon(window, Page(window).EmptyIcon, Page(window).EmptyState!, () => Fits(window), $"{w:0}x{h:0} Keranjang kosong");
        }
        window.Width = width; window.Height = height; await Settle();
        view = Page(window);
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
        Ensure(view.BackLink.Classes.Contains(CollectionView.TextLinkClass) && AutomationProperties.GetName(view.BackLink) == CollectionView.BackLabel &&
            ColorOf(view.BackLink.Foreground) == Primary && view.BackGlyph.StrokeThickness == 1.75, "Kembali ke Jelajah uses the collection back link");
        var link = Rect(view.BackLink, window); var header = Rect(view.Header, window); var firstHeading = Rect(view.Groups[0].Header, window);
        Ensure(Math.Abs(header.Y - link.Bottom - Ui.Spacing.Related) <= 1 && Math.Abs(firstHeading.Y - header.Bottom - Ui.Spacing.Related) <= 1,
            "Back link, header and content are 16 apart");

        // Items
        Ensure(view.Lines.Count == 2 && view.Groups.Count == 1 && view.ItemsPanel!.Children.Count == 1 && view.Groups[0].Lines.SequenceEqual(view.Lines),
            "One restaurant group holding one card per cart item");
        CheckGroup(window, view.Groups[0], restaurant.Name);
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

        // No summary
        Ensure(!Texts(window).Contains("Ringkasan pesanan") && !window.GetVisualDescendants().OfType<Border>().Any(b => AutomationProperties.GetName(b) == "Ringkasan pesanan") &&
            !window.GetVisualDescendants().OfType<Button>().Any(b => b.Content as string == CheckoutDialog.ConfirmLabel), "Keranjang has no summary card and no combined Pesan sekarang");
        CheckTotals(view, session, bar);
        CheckNoNotes(view);
        Ensure(view.ListScroll!.Content == view.ItemsPanel && view.ItemsPanel!.Margin.Bottom == Ui.Spacing.Page,
            "The list ends with the last group and keeps the page spacing at the bottom");
        var order = view.Groups[0].OrderButton;
        Ensure(order.Content as string == CartView.OrderLabel && order.IsEnabled && order.Classes.Contains(Ui.PrimaryClass) && order.Classes.Contains(Ui.CompactClass) &&
            !view.Groups[0].ReasonText.IsVisible && AutomationProperties.GetName(order) == $"Pesan dari {restaurant.Name}", "Each group has a compact primary Pesan button");
        await UiVerification.HoverShape(window, order, "Keranjang Pesan");

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
        Ensure(!view.Groups[0].OrderButton.IsEnabled && view.Groups[0].ReasonText.IsVisible && view.Groups[0].ReasonText.Text == CartView.UnavailableReason &&
            view.Groups[0].ReasonText.FontSize == 12 && ColorOf(view.Groups[0].ReasonText.Foreground) == Muted &&
            ColorOf(view.Groups[0].OrderButton.Background) == Divider, "An unavailable item disables Pesan and gives the muted reason under the footer");
        CheckReason(window, view.Groups[0]);
        Click(view.Groups[0].OrderButton); await Settle();
        Ensure(window.Confirmation is null, "A disabled Pesan opens no confirmation");
        first.Menu.Activate();
        var stock = first.Quantity;
        session.ChangeQuantity(first.Id, 2); first.UpdateStock(1);
        window.ShowCart(); await Settle();
        view = Page(window);
        Ensure(!view.Groups[0].OrderButton.IsEnabled && view.Groups[0].ReasonText.Text == CartView.InvalidReason && !view.Lines[0].Stepper.PlusButton.IsEnabled,
            "A group above the stock disables Pesan with a short reason");
        first.UpdateStock(stock); session.ChangeQuantity(first.Id, 1);

        // Restaurant groups
        var other = session.Listings.First(l => l.Menu.RestaurantId != restaurantId && l.IsAvailable(now) && l.Menu.IsActive && l.Quantity >= 2);
        var otherRestaurant = session.FindRestaurant(other.Menu.RestaurantId)!;
        session.AddToCart(other, 2);
        window.ShowCart(); await Settle();
        view = Page(window);
        Ensure(view.Groups.Count == 2 && view.ItemsPanel!.Children.Count == 2 && view.Groups.Select(g => g.RestaurantId).SequenceEqual([restaurantId, other.Menu.RestaurantId]) &&
            view.Groups[1].Lines.Single().Item.ListingId == other.Id, "Items are grouped per restaurant in the order they were first added");
        CheckGroup(window, view.Groups[0], restaurant.Name);
        CheckGroup(window, view.Groups[1], otherRestaurant.Name);
        Ensure(view.Groups[1].TotalText.Text == DemoSession.Rupiah(other.RescuePrice * 2), "The second group shows its own total");
        Ensure(Math.Abs(Rect(view.Groups[1].Panel, window).Y - Rect(view.Groups[0].Footer, window).Bottom - Ui.Spacing.Page) <= 1 &&
            Math.Abs(Rect(view.Groups[1].Header, window).Y - Rect(view.Groups[0].Footer, window).Bottom - Ui.Spacing.Page) <= 1, "Restaurant groups are 32 apart, footer to next heading");
        Ensure(view.Subtitle.IsVisible && view.Subtitle.Text == "2 restoran", "The header counts two restaurants");
        CheckNoNotes(view);
        CheckTotals(view, session, bar);
        Ensure(view.Groups.All(g => g.OrderButton.IsEnabled && !g.ReasonText.IsVisible), "Both valid groups can be ordered");
        other.Menu.Deactivate();
        window.ShowCart(); await Settle();
        view = Page(window);
        Ensure(!view.Groups[1].OrderButton.IsEnabled && view.Groups[1].ReasonText.Text == CartView.UnavailableReason && view.Groups[1].ReasonText.IsVisible &&
            view.Groups[0].OrderButton.IsEnabled && !view.Groups[0].ReasonText.IsVisible, "An unavailable item disables only the Pesan of its own group");
        CheckReason(window, view.Groups[1]);
        other.Menu.Activate();

        // Layout
        foreach (var listing in picks.Skip(1)) session.AddToCart(listing, 1);
        foreach (var (w, h) in new[] { (1040d, 720d), (1920d, 1080d) })
        {
            window.Width = w; window.Height = h;
            window.ShowMarketplace(); await Settle();
            var market = Rect(window.Marketplace.Body, window);
            window.ShowCart(); await Settle();
            view = Page(window);
            await CheckLayout(window, view, market, $"{w:0}x{h:0}");
        }
        Ensure(!Texts(window).Any(t => t.Contains('—')), "No em dash on Keranjang");

        // Confirmation
        window.ShowCart(); await Settle();
        view = Page(window);
        var otherGroup = view.Groups[1];
        var cartItems = session.Cart.Items.Select(i => (i.ListingId, i.Quantity)).ToList();
        var stocks = session.Cart.Items.Select(i => i.Listing.Quantity).ToList();
        var ordersBefore = session.AllOrders.Count();
        void Untouched(string label) => Ensure(session.Cart.Items.Select(i => (i.ListingId, i.Quantity)).SequenceEqual(cartItems) &&
            session.Cart.Items.Select(i => i.Listing.Quantity).SequenceEqual(stocks) && session.AllOrders.Count() == ordersBefore, label);
        async Task<CheckoutDialog> Open(CartView.Group group)
        {
            Click(group.OrderButton); await Settle();
            return window.Confirmation ?? throw new InvalidOperationException("Pesan opens the confirmation");
        }
        var dialog = await Open(otherGroup);
        var otherItems = session.Cart.FindGroup(otherRestaurant.Id)!.Items;
        Ensure(dialog.RestaurantId == otherRestaurant.Id && dialog.Title.Text == CheckoutDialog.TitleText && dialog.Title.FontWeight >= FontWeight.Bold &&
            dialog.QuestionTextBlock.Text == $"Pesan dari {otherRestaurant.Name}?", "The confirmation names the restaurant");
        Ensure(dialog.Rows.Count == otherItems.Count && dialog.Rows.Zip(otherItems).All(p => p.First.NameText.Text == p.Second.Listing.Menu.Name &&
            p.First.QuantityText.Text == $"{p.Second.Quantity} x {DemoSession.Rupiah(p.Second.UnitPrice)}" &&
            p.First.SubtotalText.Text == DemoSession.Rupiah(p.Second.CalculateSubtotal())), "The confirmation lists only that restaurant's items with quantity, unit price and subtotal");
        Ensure(dialog.TotalText.Text == DemoSession.Rupiah(other.RescuePrice * 2) && dialog.TotalText.FontWeight >= FontWeight.Bold && dialog.TotalText.FontSize >= 24 &&
            dialog.PickupText.Text!.StartsWith("Pengambilan ") && !Texts(dialog).Any(t => t.Contains("Ambil langsung di restoran") || t.Contains(PaymentNote)),
            "The confirmation shows the pickup window and the restaurant total without the pickup note");
        Ensure(dialog.Card.Width == CheckoutDialog.CardWidth && dialog.Card.CornerRadius == new CornerRadius(16) && dialog.Card.BorderThickness == default &&
            ColorOf(dialog.Card.Background) == Colors.White && dialog.Card.BoxShadow.ToString() == MarketplaceView.CardShadow.ToString() &&
            dialog.ScrimLayer.Background == ModalDialog.Scrim, "The confirmation card uses the dialog surface, radius and shadow without a border over the scrim");
        Ensure(dialog.CancelButton.Content as string == CheckoutDialog.CancelLabel && !dialog.CancelButton.Classes.Contains(Ui.PrimaryClass) &&
            dialog.ConfirmButton.Content as string == CheckoutDialog.ConfirmLabel && dialog.ConfirmButton.Classes.Contains(Ui.PrimaryClass) && dialog.ConfirmButton.IsEnabled &&
            Rect(dialog.CancelButton, window).Right <= Rect(dialog.ConfirmButton, window).X && !dialog.ErrorText.IsVisible,
            "Batal and Pesan sekarang sit on the bottom row");
        Ensure(!Texts(dialog).Any(t => t.Contains('—')), "No em dash in the confirmation");
        Ensure(window.FocusManager?.GetFocusedElement() == dialog.CloseButton && KeyboardNavigation.GetTabNavigation(dialog.Card) == KeyboardNavigationMode.Cycle,
            "Opening moves focus into the confirmation");
        dialog.ConfirmButton.Focus(NavigationMethod.Tab);
        for (var i = 0; i < 4; i++)
        {
            Press((Control)window.FocusManager!.GetFocusedElement()!, Avalonia.Input.Key.Tab); await Settle();
            Ensure(InsideCard(dialog, window.FocusManager.GetFocusedElement()), $"Tab stays inside the confirmation ({window.FocusManager.GetFocusedElement()})");
        }
        for (var i = 0; i < 4; i++)
        {
            Press((Control)window.FocusManager!.GetFocusedElement()!, Avalonia.Input.Key.Tab, KeyModifiers.Shift); await Settle();
            Ensure(InsideCard(dialog, window.FocusManager.GetFocusedElement()), "Shift+Tab stays inside the confirmation");
        }
        Click(dialog.CancelButton); await Settle();
        Ensure(window.Confirmation is null && window.FocusManager?.GetFocusedElement() == otherGroup.OrderButton, "Batal closes and returns focus to Pesan");
        Untouched("Batal creates no order");
        dialog = await Open(otherGroup);
        Press(dialog.CloseButton, Avalonia.Input.Key.Escape); await Settle();
        Ensure(window.Confirmation is null && window.FocusManager?.GetFocusedElement() == otherGroup.OrderButton, "Esc closes and returns focus to Pesan");
        Untouched("Esc creates no order");
        dialog = await Open(otherGroup);
        Click(dialog.CloseButton); await Settle();
        Ensure(window.Confirmation is null, "X closes the confirmation");
        Untouched("X creates no order");
        dialog = await Open(otherGroup);
        var corner = new Point(8, window.Bounds.Height - 8);
        dialog.ScrimLayer.RaiseEvent(new PointerPressedEventArgs(dialog.ScrimLayer, new Pointer(126, PointerType.Mouse, true), window, corner, 0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));
        await Settle();
        Ensure(window.Confirmation is null && window.FocusManager?.GetFocusedElement() == otherGroup.OrderButton, "A click on the scrim closes the confirmation");
        Untouched("The scrim creates no order");

        // Confirmation failure
        session.OrderStore = new GateStore(true);
        dialog = await Open(otherGroup);
        Click(dialog.ConfirmButton); await Settle();
        Ensure(window.Confirmation == dialog && dialog.ErrorText.IsVisible && dialog.ErrorText.Text == CartView.CheckoutFailure && dialog.ConfirmButton.IsEnabled &&
            Rect(dialog.ErrorText, window).Bottom <= Rect(dialog.ConfirmButton, window).Y + .5, "A failed save keeps the confirmation open with the existing message by the buttons");
        Untouched("A failed save changes nothing");
        dialog.Close(); await Settle();

        // Confirmation order
        var gate = new GateStore(false);
        session.OrderStore = gate;
        dialog = await Open(otherGroup);
        Click(dialog.ConfirmButton); await Task.Delay(50);
        Ensure(!dialog.ConfirmButton.IsEnabled && dialog.Submitting, "Pesan sekarang is disabled while the order is processed");
        Click(dialog.ConfirmButton); await Task.Delay(50);
        Ensure(gate.Calls == 1, "A second click does not order twice");
        gate.Release(); await Settle();
        session.OrderStore = null;
        var placed = session.LastOrder!;
        view = Page(window);
        Ensure(window.Confirmation is null && session.AllOrders.Count() == ordersBefore + 1 && placed.RestaurantId == otherRestaurant.Id &&
            placed.TotalPrice == other.RescuePrice * 2 && placed.Items.Single().ListingId == other.Id, "Pesan sekarang creates one order for that restaurant");
        Ensure(other.Quantity == stocks[cartItems.FindIndex(c => c.ListingId == other.Id)] - 2 &&
            session.Cart.Items.Select(i => (i.ListingId, i.Quantity)).SequenceEqual(cartItems.Where(c => c.ListingId != other.Id)) &&
            session.Cart.Items.Select(i => i.Listing.Quantity).SequenceEqual(cartItems.Select((c, i) => (c, i)).Where(p => p.c.ListingId != other.Id).Select(p => stocks[p.i])),
            "Only that restaurant's stock changes and the other items stay in the cart");
        Ensure(view.Groups.Count == 1 && view.Groups[0].RestaurantId == restaurantId && window.SuccessMessage == CartView.RestaurantSuccess(otherRestaurant.Name) &&
            bar.CartBadgeText.Text == session.Cart.Items.Sum(i => i.Quantity).ToString(), "Keranjang stays open with the remaining group, the badge and the notification");
        session.CancelOrder(placed);

        // Restore
        window.Width = width; window.Height = height;
        Clear(session);
        foreach (var (listing, quantity) in saved) session.AddToCart(listing, quantity);
        window.ShowMarketplace(); await Settle();
        Console.WriteLine("All Replate cart checks passed (header, restaurant groups with a divider and a footer total and Pesan, item cards, no summary card, stepper and stock limit, Hapus, unavailable per group, confirmation content, Batal, Esc, X and scrim, focus trap and return, failed save, single order per restaurant, empty state, 1040x720 and 1920x1080).");
    }

    // Checks
    private static void CheckTotals(CartView view, DemoSession session, CustomerTopBar bar)
    {
        var portions = session.Cart.Items.Sum(i => i.Quantity);
        Ensure(bar.CartBadge.IsVisible == portions > 0 && (portions == 0 || bar.CartBadgeText.Text == portions.ToString()), "The cart badge follows the cart");
        Ensure(view.Groups.All(g => g.TotalText.Text == DemoSession.Rupiah(g.Lines.Sum(l => l.Item.CalculateSubtotal()))), "Group footer totals follow the cart");
    }

    private static void CheckGroup(MainWindow window, CartView.Group group, string name)
    {
        Ensure(group.NameText.Text == name && group.NameText.FontSize == 18 && group.NameText.FontWeight >= FontWeight.Bold && ColorOf(group.NameText.Foreground) == Ink &&
            group.Header.Children.SequenceEqual(new Control[] { group.NameText }) &&
            !group.Header.GetVisualDescendants().Any(v => v == group.TotalText || v is Button),
            $"{name}: heading holds only the bold name");
        Ensure(group.Panel.Children.Count == group.Lines.Count + 3 && group.Panel.Children[^2] == group.DividerLine && group.Panel.Children[^1] == group.Footer &&
            group.DividerLine.Height == 1 && group.DividerLine.BorderThickness == default && ColorOf(group.DividerLine.Background) == Divider,
            $"{name}: a 1 px divider line and the footer follow the last item card");
        Ensure(group.TotalLabel.Text == CartView.TotalLabelText && group.TotalLabel.FontSize == 12 && ColorOf(group.TotalLabel.Foreground) == Muted &&
            group.TotalText.FontSize == CartView.TotalSize && group.TotalText.FontWeight >= FontWeight.Bold && ColorOf(group.TotalText.Foreground) == Primary &&
            group.TotalText.Text == DemoSession.Rupiah(group.Lines.Sum(l => l.Item.CalculateSubtotal())),
            $"{name}: footer total with the muted label above the large primary sum of the item subtotals");
        Ensure(group.OrderButton.Content as string == CartView.OrderLabel && group.OrderButton.Classes.Contains(Ui.PrimaryClass) && group.OrderButton.Classes.Contains(Ui.CompactClass),
            $"{name}: compact primary Pesan button");
        var header = Rect(group.Header, window); var firstCard = Rect(group.Lines[0].Card, window); var lastCard = Rect(group.Lines[^1].Card, window);
        var line = Rect(group.DividerLine, window); var row = Rect(group.FooterRow, window);
        var total = Rect(group.TotalText, window); var block = Rect(group.TotalBlock, window); var button = Rect(group.OrderButton, window);
        Ensure(Math.Abs(firstCard.Y - header.Bottom - Ui.Spacing.Related) <= 1 && Math.Abs(header.X - firstCard.X) <= 1,
            $"{name}: heading aligned with its cards, 16 above the first card");
        Ensure(Math.Abs(line.Y - lastCard.Bottom - Ui.Spacing.Related) <= 1 && line.Height <= 2 &&
            Math.Abs(line.X - firstCard.X) <= 1 && Math.Abs(line.Right - firstCard.Right) <= 1 && Math.Abs(row.Y - line.Bottom - Ui.Spacing.Related) <= 1,
            $"{name}: divider 16 under the last card, as wide as the group, footer 16 under it");
        Ensure(Math.Abs(total.X - firstCard.X) <= 1 && Math.Abs(button.Right - firstCard.Right) <= 1 && button.X >= block.Right - .5 &&
            Math.Abs(button.Center.Y - block.Center.Y) <= 1 && button.Height >= 32,
            $"{name}: total on the left edge, Pesan on the right edge, vertically centered with the total ({total}, {button})");
    }

    private static void CheckNoNotes(CartView view) =>
        Ensure(!Texts(view).Any(t => t.Contains(GroupNote) || t.Contains(PaymentNote)), "Keranjang has no pickup note and no payment note");

    private static void CheckReason(MainWindow window, CartView.Group group)
    {
        var reason = Rect(group.ReasonText, window); var row = Rect(group.FooterRow, window);
        Ensure(group.ReasonText.IsVisible && reason.Y >= row.Bottom - .5 && Math.Abs(reason.X - row.X) <= 1 && group.Footer.Children.IndexOf(group.ReasonText) == 1,
            $"{group.NameText.Text}: the reason sits left-aligned under the footer row");
    }

    private static async Task CheckLayout(MainWindow window, CartView view, Rect market, string size)
    {
        var body = Rect(view.Body, window);
        var scroll = view.ListScroll!;
        Ensure(Math.Abs(body.X - market.X) <= 1 && Math.Abs(body.Right - market.Right) <= 1, $"{size}: Keranjang edges line up with Jelajah ({body} vs {market})");
        Ensure(!window.GetVisualDescendants().OfType<Border>().Any(b => AutomationProperties.GetName(b) == "Ringkasan pesanan"), $"{size}: no summary card");
        Ensure(scroll.Extent.Width <= scroll.Viewport.Width + .5 && window.GetVisualDescendants().OfType<ScrollViewer>().Where(s => s.IsEffectivelyVisible)
            .All(s => s.Extent.Width <= s.Viewport.Width + .5), $"{size}: no horizontal scrolling");
        foreach (var line in view.Lines)
        {
            var card = Rect(line.Card, window);
            if (card.Bottom > Rect(scroll, window).Bottom) continue;
            Ensure(Math.Abs(card.X - body.X) <= 1 && Math.Abs(card.Right - body.Right) <= 1, $"{size}: {line.NameText.Text} card fills the content width");
            var parts = new Control[] { line.Photo, line.NameText, line.MetaText, line.PickupText, line.PriceText, line.Stepper, line.SubtotalText, line.RemoveButton }
                .Select(c => (c, Rect(c, window))).ToList();
            Ensure(parts.All(p => Inside(p.Item2, card)), $"{size}: {line.NameText.Text} content stays inside its card");
            var columns = new[] { Rect(line.Photo, window), Union(line), Rect(line.Stepper, window), Rect(line.SubtotalText, window), Rect(line.RemoveButton, window) };
            for (var i = 1; i < columns.Length; i++)
                Ensure(columns[i].X >= columns[i - 1].Right - .5, $"{size}: {line.NameText.Text} columns do not overlap ({columns[i - 1]} and {columns[i]})");
        }
        foreach (var group in view.Groups)
        {
            var header = Rect(group.Header, window);
            if (header.Bottom <= Rect(scroll, window).Bottom)
                Ensure(Math.Abs(header.X - body.X) <= 1 && Math.Abs(header.Right - body.Right) <= 1 &&
                    Inside(Rect(group.NameText, window), header), $"{size}: {group.NameText.Text} heading spans the content width without overlap");
            group.Footer.BringIntoView(); await Settle();
            var row = Rect(group.FooterRow, window); var visible = Rect(scroll, window);
            Ensure(Inside(row, visible) && Math.Abs(row.X - body.X) <= 1 && Math.Abs(row.Right - body.Right) <= 1 &&
                new Control[] { group.TotalLabel, group.TotalText, group.OrderButton }.All(c => Inside(Rect(c, window), row)) &&
                Rect(group.OrderButton, window).X >= Rect(group.TotalBlock, window).Right - .5, $"{size}: {group.NameText.Text} footer is fully visible without overlap");
            CheckGroup(window, group, group.NameText.Text!);
        }
        scroll.Offset = default; await Settle();
        Ensure(Rect(view.Header, window).Bottom <= Rect(view.Groups[0].Header, window).Y + .5 && Rect(view.BackLink, window).Bottom <= Rect(view.Header, window).Y + .5,
            $"{size}: header parts do not overlap");

        var group0 = view.Groups[0];
        group0.OrderButton.BringIntoView(); await Settle();
        Click(group0.OrderButton); await Settle();
        var dialog = window.Confirmation ?? throw new InvalidOperationException($"{size}: Pesan opens the confirmation");
        var card0 = Rect(dialog.Card, window); var bounds = window.Bounds;
        Ensure(card0.X >= ModalDialog.Inset - .5 && card0.Y >= ModalDialog.Inset - .5 && card0.Right <= bounds.Width - ModalDialog.Inset + .5 &&
            card0.Bottom <= bounds.Height - ModalDialog.Inset + .5 && Math.Abs(card0.X - (bounds.Width - card0.Right)) < 1 && Math.Abs(card0.Y - (bounds.Height - card0.Bottom)) < 1 &&
            Math.Abs(card0.Width - CheckoutDialog.CardWidth) < .5, $"{size}: the confirmation is centered and fits inside the window ({card0})");
        Ensure(Inside(Rect(dialog.ConfirmButton, window), card0) && Inside(Rect(dialog.CancelButton, window), card0) &&
            dialog.Scroll.Extent.Width <= dialog.Scroll.Viewport.Width + .5, $"{size}: the confirmation buttons are not cut off and nothing scrolls sideways");
        dialog.Close(); await Settle();
        Rect Union(CartView.Line line) => new[] { line.NameText, line.MetaText, line.PickupText, line.PriceText }.Select(t => Rect(t, window)).Aggregate((a, b) => a.Union(b));
    }

    // Helpers
    private static void Clear(DemoSession session)
    {
        foreach (var item in session.Cart.Items.ToList()) session.ChangeQuantity(item.ListingId, 0);
    }

    private static CartView Page(MainWindow window) => window.CurrentPage as CartView ?? throw new InvalidOperationException("Keranjang is open");

    private static List<string> Texts(Visual root) => root.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").ToList();

    private static bool Fits(MainWindow window) => window.CurrentPage is { } page && page.Bounds.Width <= window.Bounds.Width + .5 &&
        window.GetVisualDescendants().OfType<ScrollViewer>().Where(s => s.IsEffectivelyVisible).All(s => s.Extent.Width <= s.Viewport.Width + .5);

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

    private static void Press(Control target, Key key, KeyModifiers modifiers = KeyModifiers.None) =>
        target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key, KeyModifiers = modifiers, Source = target });

    private static bool InsideCard(ModalDialog dialog, IInputElement? element) => element is Visual visual && dialog.Card.IsVisualAncestorOf(visual);

    private static async Task Settle()
    { await Task.Delay(250); await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render); }

    private static void Ensure(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }

    private sealed class GateStore(bool fail) : Data.IOrderStore
    {
        private readonly TaskCompletionSource gate = new();
        internal int Calls { get; private set; }

        public Task SaveOrderAsync(Order order)
        {
            Calls++;
            return fail ? Task.FromException(new InvalidOperationException("PostgreSQL tidak tersedia.")) : gate.Task;
        }

        internal void Release() => gate.SetResult();
    }
}
