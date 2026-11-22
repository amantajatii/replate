using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Replate.Domain.Entities;
using Replate.Domain.Enums;

namespace Replate.Desktop;

internal static class CustomerOrdersVerification
{
    private static readonly Color Primary = Color.Parse("#1F6B4F");
    private static readonly Color Selection = Color.Parse("#DDEDE5");
    private static readonly Color Muted = Color.Parse("#69736D");

    internal static async Task Run(string output)
    {
        var session = new DemoSession().AsCustomer();
        Ensure(session.Restaurant.Orders.Count > 0 && !session.CustomerOrders.Any(), "Other customers' history does not count as the current customer's history");
        var window = new MainWindow(session) { Width = 1040, Height = 720 };
        try
        {
            window.Show(); await Settle();
            OrdersView Page() => (OrdersView)window.CurrentPage!;
            OrderDetailView Detail() => (OrderDetailView)window.CurrentPage!;
            void Shot(string name)
            {
                using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height), new Vector(96, 96));
                bitmap.Render(window); bitmap.Save(Path.Combine(output, name + ".png"), PngBitmapEncoderOptions.Default);
            }
            async Task Size(double w, double h) { window.WindowState = WindowState.Normal; window.Width = w; window.Height = h; await Settle(); }

            // Empty history
            foreach (var (w, h) in Sizes)
            {
                await Size(w, h); window.ShowCustomerOrders(); await Settle();
                var page = Page();
                CheckHeader(window, page, $"{w:0}x{h:0} empty");
                Ensure(!page.FilterRow!.IsVisible && page.Cards.Count == 0 && page.EmptyHeading?.Text == OrdersView.EmptyTitle &&
                    page.EmptyState!.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == OrdersView.EmptyMessage) &&
                    page.EmptyButton?.Content as string == OrdersView.EmptyAction && page.EmptyButton.Classes.Contains(Ui.PrimaryClass) &&
                    page.EmptyIcon is { Stroke: ISolidColorBrush { Color: var stroke } } && stroke == Primary,
                    "No history shows the icon, Belum ada pesanan, its sentence and the primary Jelajahi menu button without filters");
                Inside(window, page.EmptyState, "Empty state");
                await UiVerification.EmptyIcon(window, page.EmptyIcon, page.EmptyState!, () => page.Bounds.Width <= window.Bounds.Width + .5 &&
                    window.GetVisualDescendants().OfType<ScrollViewer>().Where(s => s.IsEffectivelyVisible).All(s => s.Extent.Width <= s.Viewport.Width + .5), $"{w:0}x{h:0} Pesanan saya kosong");
                Shot($"customer-orders-empty-{w:0}x{h:0}");
            }
            Click(Page().EmptyButton!); await Settle();
            Ensure(window.CurrentPage is MarketplaceView, "Jelajahi menu opens Jelajah");

            // Filter empty
            var bumi = session.Restaurants.Single(r => r.Name == "Dapur Bumi");
            var sambal = session.Restaurants.Single(r => r.Name == "Warung Sambal Ijo");
            var pagi = session.Restaurants.Single(r => r.Name == "Roti Pagi Kotabaru");
            List<SurplusListing> Offers(Restaurant r) => session.Listings.Where(l => l.Menu.RestaurantId == r.Id && l.IsAvailable(DateTime.UtcNow) && l.Menu.IsActive).ToList();
            async Task<Order> Place(Restaurant r, int count)
            {
                foreach (var offer in Offers(r).Take(count)) session.AddToCart(offer, 1);
                var placed = session.Checkout(r.Id);
                await Task.Delay(30);
                return placed;
            }
            var large = await Place(bumi, 4);
            Ensure(large.Items.Count == 4, "Dapur Bumi order holds four menus");
            foreach (var (w, h) in Sizes)
            {
                await Size(w, h); window.ShowCustomerOrders(); await Settle();
                foreach (var filter in new[] { OrdersView.Done, OrdersView.Cancelled })
                {
                    Click(FilterButton(Page(), filter)); await Settle();
                    var page = Page();
                    Ensure(page.SelectedFilter == filter && page.Cards.Count == 0 && page.FilterRow!.IsVisible && page.EmptyHeading?.Text == OrdersView.FilterEmptyTitle &&
                        page.EmptyButton?.Content as string == OrdersView.FilterEmptyAction && page.EmptyButton.Classes.Contains(Ui.PrimaryClass) &&
                        !page.EmptyButton.Classes.Contains(CollectionView.TextLinkClass), $"{w:0}x{h:0} {filter} without results shows Tidak ada pesanan di sini with the primary Lihat semua pesanan button");
                    Inside(window, page.EmptyState!, $"{w:0}x{h:0} {filter} empty state");
                    Shot($"customer-orders-filter-empty-{filter}-{w:0}x{h:0}");
                    await UiVerification.FilterEmptyButton(window, page, () => page.Bounds.Width <= window.Bounds.Width + .5 &&
                        window.GetVisualDescendants().OfType<ScrollViewer>().Where(s => s.IsEffectivelyVisible).All(s => s.Extent.Width <= s.Viewport.Width + .5),
                        $"{w:0}x{h:0} {filter} Lihat semua pesanan");
                    Click(page.EmptyButton!); await Settle();
                    Ensure(Page().SelectedFilter == OrdersView.All && Page().Cards.Count == 1 && FilterButton(Page(), OrdersView.All).Classes.Contains(OrdersView.SelectedClass),
                        "Lihat semua pesanan returns to Semua");
                }
            }

            // Orders from several restaurants
            var single = await Place(sambal, 1);
            var cancelled = await Place(pagi, 2);
            session.CancelOrder(cancelled);
            var completed = await Place(bumi, 1);
            session.AsOwner();
            session.ConfirmOrder(completed, 15); session.ReadyOrder(completed); session.CompletePickup(completed, completed.PickupCode);
            session.AsCustomer();
            var newest = new[] { completed, cancelled, single, large };
            var expected = new Dictionary<string, Order[]>
            {
                [OrdersView.All] = newest,
                [OrdersView.Active] = [single, large],
                [OrdersView.Done] = [completed],
                [OrdersView.Cancelled] = [cancelled]
            };
            foreach (var (w, h) in Sizes)
            {
                var size = $"{w:0}x{h:0}";
                await Size(w, h); window.ShowCustomerOrders(); await Settle();
                CheckHeader(window, Page(), size);
                Ensure(Page().SelectedFilter == OrdersView.All, $"{size}: Semua is the default filter");
                foreach (var filter in OrdersView.Filters)
                {
                    Click(FilterButton(Page(), filter)); await Settle();
                    var page = Page();
                    Ensure(page.Cards.Select(c => c.Order).SequenceEqual(expected[filter]) &&
                        page.Cards.Zip(page.Cards.Skip(1)).All(p => p.First.Order.OrderedAt >= p.Second.Order.OrderedAt) &&
                        page.Cards.All(c => OrdersView.InFilter(c.Order, filter)),
                        $"{size} {filter}: cards follow the status mapping, newest first");
                    CheckFilters(page, filter, size);
                    foreach (var card in page.Cards) CheckCard(window, session, page, card, $"{size} {filter}");
                    Ensure(page.ListScroll!.Extent.Width <= page.ListScroll.Viewport.Width + .5, $"{size} {filter}: no horizontal scrolling");
                    Shot($"customer-orders-{filter}-{size}");
                }
                Click(FilterButton(Page(), OrdersView.All)); await Settle();
                var all = Page();
                Ensure(all.ItemsPanel!.Spacing == Ui.Spacing.Related, $"{size}: cards are 16 apart");
                var first = all.Cards[0].Card;
                Ensure(Math.Abs(Rect(first, window).Y - Rect(all.FilterRow!, window).Bottom - Ui.Spacing.Related) < 1 &&
                    Math.Abs(Rect(first, window).X - Rect(all.Title!, window).X) < 1, $"{size}: the first card sits 16 under the filters on the content edge");
                var split = all.Cards.Where(c => c.Order == large || c.Order == single).ToList();
                Ensure(split.Count == 2 && split[0].Card != split[1].Card && split.Select(c => c.NameText.Text).ToHashSet().SetEquals([bumi.Name, sambal.Name]),
                    $"{size}: orders from two restaurants are two separate cards");
                var large4 = all.Cards.Single(c => c.Order == large);
                Ensure(large4.ItemRows.Count == OrdersView.SummaryRows && large4.MoreLine?.Text == "+1 menu lainnya", $"{size}: four menus show three rows and +1 menu lainnya");
                await UiVerification.TextLink(window, all.BackLink!, () => all.ListScroll!.Extent.Width <= all.ListScroll.Viewport.Width + .5, $"{size} Pesanan saya");
                foreach (var button in all.FilterButtons.Take(2)) await UiVerification.HoverShape(window, button, $"{size} filter {button.Content}");
                await UiVerification.TextLink(window, all.Cards[0].DetailButton, () => all.ListScroll!.Extent.Width <= all.ListScroll.Viewport.Width + .5,
                    $"{size} Lihat detail", OrdersView.DetailLabel);
                foreach (var button in all.FilterButtons)
                {
                    var ring = TopBarVerification.FocusRing(button);
                    ((IPseudoClasses)button.Classes).Set(":focus-visible", true); await Settle();
                    Ensure(ring.BorderBrush is ISolidColorBrush { Color: var ringColor } && ringColor == Primary && ring.IsVisible &&
                        ring.CornerRadius.TopLeft > button.CornerRadius.TopLeft, $"{size} filter {button.Content}: the shared focus ring follows the pill");
                    ((IPseudoClasses)button.Classes).Set(":focus-visible", false);
                }
            }

            // Detail
            foreach (var (w, h) in Sizes)
            {
                var size = $"{w:0}x{h:0}";
                await Size(w, h);
                foreach (var order in newest)
                {
                    window.ShowCustomerOrders(); await Settle();
                    Click(Page().Cards.Single(c => c.Order == order).DetailButton); await Settle();
                    Ensure(window.CurrentPage is OrderDetailView, $"{size}: Lihat detail opens the order");
                    CheckDetail(window, session, Detail(), order, $"{size} {order.Status}");
                    Shot($"customer-order-detail-{order.Status}-{size}");
                }
                var view = Detail();
                await UiVerification.TextLink(window, view.BackLink!, () => view.Scroll!.Extent.Width <= view.Scroll.Viewport.Width + .5,
                    $"{size} Detail pesanan", OrderDetailView.BackText);
                window.ShowOrder(large, false); await Settle();
                await UiVerification.HoverShape(window, Detail().CopyCodeButton!, $"{size} copy");
                Click(Detail().BackLink!); await Settle();
                Ensure(window.CurrentPage is OrdersView, $"{size}: Kembali ke Pesanan saya opens the orders page");
            }
            Console.WriteLine("All Replate customer order checks passed (cards, status filters and mapping, newest first, empty and filter-empty states, detail layout, minimum/1920x1080).");
        }
        finally { window.Close(); }
    }

    private static readonly (double, double)[] Sizes = [(1040, 720), (1920, 1080)];

    // Checks
    private static void CheckHeader(Window window, OrdersView page, string label)
    {
        Ensure(page.BackLink!.Classes.Contains(CollectionView.TextLinkClass) &&
            page.BackLink.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == CollectionView.BackLabel), $"{label}: Kembali ke Jelajah uses the shared text link");
        Ensure(page.Title!.Text == OrdersView.TitleText && page.Title.FontSize == MarketplaceView.TitleSize && page.Title.FontWeight == FontWeight.Bold,
            $"{label}: Pesanan saya is 32 bold like Keranjang");
        var back = Rect(page.BackLink, window); var title = Rect(page.Title, window);
        Ensure(Math.Abs(title.Y - back.Bottom - Ui.Spacing.Related) < 1 && Math.Abs(back.Y - (window as MainWindow)!.TopBar.Bounds.Height - Ui.Spacing.Section) < 1,
            $"{label}: 24 from the top bar to the back link and 16 to the title ({back}, {title})");
        var column = Math.Min(MarketplaceView.ContentMaxWidth, window.Bounds.Width - 2 * Ui.Spacing.Page);
        Ensure(Math.Abs(page.Body!.Bounds.Width - column) < 1 && Math.Abs(Rect(page.Body, window).X - (window.Bounds.Width - column) / 2) < 1,
            $"{label}: the page uses the Jelajah content column");
        if (page.FilterRow!.IsVisible)
            Ensure(Math.Abs(Rect(page.FilterRow, window).Y - title.Bottom - Ui.Spacing.Related) < 1, $"{label}: the filters sit 16 under the title");
    }

    private static void CheckFilters(OrdersView page, string selected, string label)
    {
        Ensure(page.FilterButtons.Select(b => b.Content as string).SequenceEqual(OrdersView.Filters) &&
            page.FilterButtons.Select(b => b.Bounds.Y).Distinct().Count() == 1, $"{label}: Semua, Aktif, Selesai, Dibatalkan in one row");
        foreach (var button in page.FilterButtons)
        {
            var presenter = Presenter(button);
            var on = button.Content as string == selected;
            Ensure(button.Classes.Contains(OrdersView.SelectedClass) == on && button.CornerRadius == new CornerRadius(button.Bounds.Height / 2) &&
                presenter.CornerRadius == button.CornerRadius && !button.ClipToBounds &&
                Fill(presenter.Background) == (on ? Selection : Colors.Transparent) && Fill(presenter.Foreground) == (on ? Primary : Muted) &&
                button.FontWeight == (on ? FontWeight.SemiBold : FontWeight.Normal) && Math.Abs((button.RenderTransform?.Value.M11 ?? 1) - 1) < .005,
                $"{label} filter {button.Content}: {(on ? "selection fill with bold primary text" : "transparent with muted text")} on a pill");
        }
    }

    private static void CheckCard(Window window, DemoSession session, OrdersView page, OrdersView.OrderCard card, string label)
    {
        var order = card.Order;
        Ensure(card.Card.Background is ISolidColorBrush { Color: var white } && white == Colors.White && card.Card.CornerRadius == new CornerRadius(16) &&
            card.Card.BoxShadow.ToString() == MarketplaceView.CardShadow.ToString() && card.Card.BorderThickness == default, $"{label}: white card, radius 16, soft shadow, no border");
        Ensure(card.NameText.Text == session.FindRestaurant(order.RestaurantId)!.Name && card.NameText.FontWeight == FontWeight.Bold &&
            card.Badge.Status == order.Status && ((TextBlock)card.Badge.Child!).Text == DemoSession.Status(order) &&
            Rect(card.Badge, window).X > Rect(card.NameText, window).X, $"{label}: restaurant name on the left, status badge on the right");
        Ensure(card.Meta.Text == OrdersView.MetaText(order) && card.Meta.Text!.Contains(OrdersView.OrderedText(order)) && !card.Meta.Text.Contains('—'),
            $"{label}: order time and pickup window in muted text");
        var shown = order.Items.Take(OrdersView.SummaryRows).ToList();
        Ensure(card.ItemRows.Count == shown.Count && card.ItemRows.Zip(shown).All(p =>
                p.First.Children.OfType<TextBlock>().Select(t => t.Text).SequenceEqual([p.Second.MenuName, $"{p.Second.Quantity} x"])) &&
            (order.Items.Count > OrdersView.SummaryRows ? card.MoreLine?.Text == OrdersView.MoreText(order.Items.Count - OrdersView.SummaryRows) : card.MoreLine is null),
            $"{label}: up to three item rows and the +N line");
        Ensure(OrdersView.IsActive(order) ? card.CodeText?.Text == order.PickupCode && card.CodeText.FontFamily == Ui.CodeFont &&
                !card.CodeBlock!.GetVisualDescendants().OfType<Button>().Any() : card.CodeBlock is null,
            $"{label}: pickup code only on active orders, without a copy button");
        Ensure(card.TotalText.Text == DemoSession.Rupiah(order.Items.Sum(i => i.CalculateSubtotal())) && card.TotalText.Text == DemoSession.Rupiah(order.TotalPrice) &&
            card.TotalText.FontSize == OrdersView.TotalSize && card.TotalText.FontWeight == FontWeight.Bold && Fill(card.TotalText.Foreground) == Primary &&
            card.TotalLabel.Text == CartView.TotalLabelText, $"{label}: Total with the right large primary amount");
        Ensure(card.DetailButton.Content is TextBlock { Text: OrdersView.DetailLabel } && card.DetailButton.Classes.Contains(CollectionView.TextLinkClass) &&
            !card.DetailButton.Classes.Contains(Ui.PrimaryClass) && card.FooterRow.Children.Contains(card.DetailButton), $"{label}: Lihat detail text link in the footer");
        if (!card.Card.IsEffectivelyVisible || card.Card.Bounds.Height == 0) return;
        var box = Rect(card.Card, window);
        var list = Rect(page.ListScroll!, window);
        var detail = Rect((TextBlock)card.DetailButton.Content!, window);
        Ensure(Math.Abs(detail.Right - (box.Right - OrdersView.CardPadding)) < 1.5 && Math.Abs(Rect(card.DetailButton, window).Center.Y - Rect(card.TotalText.Parent as Control ?? card.TotalText, window).Center.Y) < 1.5,
            $"{label}: Lihat detail ends on the card content edge, centered with Total ({detail})");
        Ensure(box.X >= 0 && box.Right <= window.Bounds.Width - Ui.Spacing.Page + 1 && Rect(card.DetailButton, window).Right <= box.Right - OrdersView.CardPadding + 8 + 1 &&
            Rect(card.DividerLine, window).Width > box.Width - 2 * OrdersView.CardPadding - 1 && box.X >= list.X, $"{label}: the card and its footer are not cut off sideways");
    }

    private static void CheckDetail(Window window, DemoSession session, OrderDetailView view, Order order, string label)
    {
        Ensure(view.BackLink!.Classes.Contains(CollectionView.TextLinkClass) &&
            view.BackLink.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == OrderDetailView.BackText), $"{label}: Kembali ke Pesanan saya");
        Ensure(view.Title!.Text == OrderDetailView.TitleText && view.Title.FontSize == MarketplaceView.TitleSize && view.Badge!.Status == order.Status &&
            window.GetVisualDescendants().OfType<OrderStatusBadge>().Count() == 1 && Rect(view.Badge, window).X > Rect(view.Title, window).Right &&
            Math.Abs(Rect(view.Badge, window).Center.Y - Rect(view.Title, window).Center.Y) < 2, $"{label}: Detail pesanan with the status badge beside it");
        var items = Rect(view.ItemsCard!, window); var side = Rect(view.Side!, window);
        var share = items.Width / (side.Right - items.X);
        Ensure(view.ColumnsGrid!.ColumnDefinitions.Count == 2 && share > .55 && share < .65 && Math.Abs(items.Y - side.Y) < 1 && side.X > items.Right,
            $"{label}: two columns, the items about 60% on the left ({share:0.00})");
        Ensure(view.Side!.Children[0] == view.CodeCard && view.Side.Children[1] == view.InfoCard, $"{label}: the pickup code card above the restaurant card on the right");
        Ensure(view.PickupCodeText!.Text == order.PickupCode && view.PickupCodeText.FontSize == OrderDetailView.CodeSize &&
            view.PickupCodeText.FontWeight == FontWeight.Bold && view.PickupCodeText.FontFamily == Ui.CodeFont &&
            view.CodeCard!.GetVisualDescendants().Contains(view.PickupCodeText), $"{label}: the large fixed-width pickup code");
        Ensure((view.CopyCodeButton is not null) == (order.Status != OrderStatus.Cancelled && !string.IsNullOrWhiteSpace(order.PickupCode)) &&
            (view.CopyCodeButton is null || view.CodeCard!.GetVisualDescendants().Contains(view.CopyCodeButton)) &&
            (order.Status == OrderStatus.Cancelled ? view.CodeCaptionText is null : view.CodeCaptionText?.Text == OrderDetailView.CodeCaption),
            $"{label}: the copy button follows the existing rule");
        var restaurant = session.FindRestaurant(order.RestaurantId)!;
        var info = view.InfoCard!.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
        Ensure(view.RestaurantText!.Text == restaurant.Name && info.Contains(OrdersView.PickupWindow(order)) && info.Contains(OrdersView.OrderedText(order)),
            $"{label}: restaurant name, pickup window and order time");
        Ensure(view.ItemRows.Count == order.Items.Count && view.ItemRows.All(r => r.Name.Text == r.Item.MenuName &&
                r.Quantity.Text == $"{r.Item.Quantity} x {DemoSession.Rupiah(r.Item.UnitPrice)}" && r.Subtotal.Text == DemoSession.Rupiah(r.Item.CalculateSubtotal()) &&
                Rect(r.Subtotal, window).Right > Rect(r.Name, window).Right) &&
            view.TotalText!.Text == DemoSession.Rupiah(order.Items.Sum(i => i.CalculateSubtotal())) && view.TotalText.Text == DemoSession.Rupiah(order.TotalPrice) &&
            view.TotalText.FontSize == CartView.TotalSize && Fill(view.TotalText.Foreground) == Primary, $"{label}: item rows, subtotals and the total");
        Ensure((view.CancelOrderButton is not null) == OrdersView.IsActive(order), $"{label}: cancellation stays available only for active orders");
        Ensure(view.Scroll!.Extent.Width <= view.Scroll.Viewport.Width + .5 && side.Right <= window.Bounds.Width - Ui.Spacing.Page + 1 && items.X >= Ui.Spacing.Page - 1,
            $"{label}: no horizontal scrolling and nothing cut off");
        if (view.CopyCodeButton is { } copy)
            Ensure(Rect(copy, window).Right <= Rect(view.CodeCard!, window).Right - OrderDetailView.CardPadding + 1, $"{label}: the copy button fits inside the code card");
    }

    // Helpers
    private static Button FilterButton(OrdersView page, string label) => page.FilterButtons.Single(b => b.Content as string == label);
    private static void Inside(Window window, Control control, string label)
    {
        var box = Rect(control, window);
        Ensure(box.X >= 0 && box.Y >= 0 && box.Right <= window.Bounds.Width && box.Bottom <= window.Bounds.Height, $"{label} fits the window");
    }
    private static Rect Rect(Visual visual, Visual root) => new(visual.TranslatePoint(default, root) ?? default, visual.Bounds.Size);
    private static ContentPresenter Presenter(Button button) =>
        button.GetVisualDescendants().OfType<ContentPresenter>().First(p => p.Name == "PART_ContentPresenter");
    private static Color Fill(IBrush? brush) => brush is ISolidColorBrush solid ? solid.Color : Colors.Transparent;
    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static async Task Settle() { await Task.Delay(300); await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render); }
    private static void Ensure(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
}
