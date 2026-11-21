using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Path = Avalonia.Controls.Shapes.Path;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Replate.Desktop;

internal static class MarketplaceVerification
{
    private static readonly Color Primary = Color.Parse("#245B43");
    private static readonly Color Selection = Color.Parse("#E0E9DF");

    internal static async Task Run(MainWindow window)
    {
        var session = window.Session;
        var view = window.Marketplace;
        Ensure(window.CurrentPage == view && window.SearchText == "", "Jelajah starts with an empty search");

        // Banner
        var name = session.CurrentUser!.Name;
        Ensure(view.Banner.Greeting.Text == $"Halo, {name}." && view.Banner.Message.Text == "Yuk, Selamatkan Makanan Lewat Setiap Pesanan!",
            "Banner greets the signed-in user with the call to action");
        Ensure(view.Body.Children[0] == view.Banner && view.Body.Children[1] == view.CollectionRow && view.Body.Children[2] == view.Section &&
            view.Section.Children[0] == view.TitleRow && view.Section.Children[1] == view.ListingsPanel, "Banner, filter cards, then the Jelajahi Menu section");
        Ensure(!view.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text?.Contains("menu tersedia") == true), "The N menu tersedia counter is gone");
        Ensure(view.Title.Text == MarketplaceView.SectionTitle && view.Title.FontSize >= 30 && view.Title.FontWeight >= FontWeight.Bold &&
            ColorOf(view.Title.Foreground) == Color.Parse("#23382C") && view.Title.HorizontalAlignment == Avalonia.Layout.HorizontalAlignment.Left,
            $"Jelajahi Menu is a bold, left-aligned section title in ink ({view.Title.FontSize:0})");
        Ensure(!view.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text is "Semua restoran" or "Makanan tersedia" || t.Text?.Contains('—') == true),
            "Decorative section titles are removed and no text uses an em dash");

        // Filter cards
        Ensure(view.CollectionCards.Keys.SequenceEqual(MarketplaceView.CollectionNames) && !view.CollectionCards.ContainsKey("Semua"), "Five collection cards without Semua");
        foreach (var (label, card) in view.CollectionCards)
        {
            var icon = card.GetVisualDescendants().OfType<Path>().Single();
            var frame = card.GetVisualDescendants().OfType<Border>().First();
            Ensure(icon.StrokeThickness == 1.75 && icon.StrokeLineCap == PenLineCap.Round && ColorOf(icon.Stroke) == Primary, $"{label}: primary line icon");
            Ensure(card.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == label), $"{label}: label under the icon");
            Ensure(frame.CornerRadius == new CornerRadius(16) && frame.BoxShadow.ToString() == MarketplaceView.CardShadow.ToString() &&
                ColorOf(card.Background) == Colors.White && ColorOf(card.BorderBrush) == Colors.Transparent && !card.ClipToBounds, $"{label}: white card with the card shadow and no border");
            Ensure(card.Focusable && KeyboardNavigation.GetIsTabStop(card), $"{label}: reachable with Tab");
            var ring = TopBarVerification.FocusRing(card);
            Ensure(card.FocusAdorner == Ui.NoFocusAdorner && ColorOf(ring.BorderBrush) == Colors.Transparent, $"{label}: no default focus adorner");
            await Pseudo(card, ":focus-visible", true);
            Ensure(ColorOf(ring.BorderBrush) == Primary && ring.BorderThickness == new Thickness(Ui.FocusRingWidth) && ColorOf(card.BorderBrush) == Colors.Transparent,
                $"{label}: keyboard focus shows a 2 px primary ring");
            await Pseudo(card, ":focus-visible", false);
        }
        var first = view.CollectionCards[MarketplaceView.Cheapest];
        await Pseudo(first, ":pointerover", true);
        Ensure(ColorOf(first.Background) == Selection, "Filter card hover uses the selection fill");
        await Pseudo(first, ":pointerover", false);

        var all = view.VisibleCount;
        Ensure(view.VisibleListings.SequenceEqual(ListingQuery.Available(session)) && view.CollectionCards.Values.All(c => !c.Classes.Contains("active")),
            "Jelajah lists every available listing in the default order with no selected card");
        Ensure(!typeof(MarketplaceView).GetMembers().Any(m => m.Name is "ActiveFilter" or "SelectFilter" or "ClearFilter") &&
            !view.GetVisualDescendants().OfType<Button>().Any(b => b.Content as string == "Hapus filter"), "Pick-one filter state and Hapus filter are gone");

        // Listing cards
        var cards = view.ListingsPanel.Children.OfType<Border>().ToList();
        Ensure(cards.Count == all && view.ListingsPanel.Spacing == 16, "Every listing is a card, 16 apart");
        Ensure(cards.All(c => c.CornerRadius == new CornerRadius(16) && c.BoxShadow.ToString() == MarketplaceView.CardShadow.ToString() &&
            ColorOf(c.Background) == Colors.White && c.BorderThickness == default), "Listing cards are white with the card shadow and no border");
        Ensure(!view.GetVisualDescendants().OfType<Border>().Any(b => b.BorderThickness == new Thickness(0, 0, 0, 1)), "Old row separators are gone");
        foreach (var listing in view.VisibleListings.Take(1))
        {
            var texts = cards[0].GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
            Ensure(texts.Contains(DemoSession.Rupiah(listing.RescuePrice)) && texts.Contains("Pengambilan") && texts.Contains($"{listing.Quantity} porsi tersisa") &&
                texts.Contains($"Hemat {listing.CalculateDiscount():0}%") && cards[0].GetVisualDescendants().OfType<MenuPhotoView>().Any(),
                "Listing card keeps photo, prices, pickup and savings");
        }
        var add = cards[0].GetVisualDescendants().OfType<Button>().Last();
        var addRing = TopBarVerification.FocusRing(add);
        Ensure(add.FocusAdorner == Ui.NoFocusAdorner && ColorOf(addRing.BorderBrush) == Colors.Transparent, "Tambah uses the shared focus ring");
        await Pseudo(add, ":focus-visible", true);
        Ensure(ColorOf(addRing.BorderBrush) == Primary && !TopBarVerification.HasAdorner(add), "Tambah keyboard focus shows the primary ring, no black box");
        await Pseudo(add, ":focus-visible", false);
        Ensure(view.Body.Children[^1] is TextBlock { Text: "Harga dan stok contoh. Reservasi tidak melibatkan pembayaran." }, "Sample note stays under the list");
        Ensure(view.Scroll.HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled, "Jelajah never scrolls horizontally");

        // Sizes
        var (width, height) = (window.Width, window.Height);
        window.Width = 1040; window.Height = 720; await Settle();
        CheckLayout(view, "window at 1040x720", 1040);
        CheckRhythm(window, window.TopBar, view, "window at 1040x720");
        window.Width = width; window.Height = height; await Settle();
        foreach (var size in new[] { new Size(1040, 720), new Size(1360, 940), new Size(1920, 1080) })
        {
            var sample = new MarketplaceView(window, session);
            var host = new Border { Child = sample, Width = size.Width, Height = size.Height - CustomerTopBar.BarHeight };
            var frame = new Window { Width = 1040, Height = 720, Content = new Canvas { Children = { host } } };
            try
            {
                frame.Show(); sample.Refresh(); await Settle();
                CheckLayout(sample, $"{size.Width:0}x{size.Height:0}", size.Width);
            }
            finally { frame.Close(); }
        }
        foreach (var size in new[] { new Size(1040, 720), new Size(1360, 940), new Size(1920, 1080) })
        {
            var bar = new CustomerTopBar(() => { }, () => { }, () => { }, () => { }, _ => { });
            var sample = new MarketplaceView(window, session);
            DockPanel.SetDock(bar, Dock.Top);
            var shell = new DockPanel { Width = size.Width, Height = size.Height, Children = { bar, sample } };
            shell.Styles.Add(Ui.FocusStyles());
            var frame = new Window { Width = 1040, Height = 720, Content = new Canvas { Children = { shell } } };
            try
            {
                frame.Show(); sample.Refresh(); await Settle();
                CheckRhythm(shell, bar, sample, $"{size.Width:0}x{size.Height:0}");
            }
            finally { frame.Close(); }
        }
        var realName = session.CurrentUser.Name;
        try
        {
            session.CurrentUser.Name = "Pelanggan Dengan Nama Lengkap Yang Sangat Panjang Sekali Untuk Uji Elipsis";
            var sample = new MarketplaceView(window, session);
            var frame = new Window { Width = 1040, Height = 720, Content = new Canvas { Children = { new Border { Child = sample, Width = 1040, Height = 648 } } } };
            try
            {
                frame.Show(); sample.Refresh(); await Settle();
                var greeting = sample.Banner.Greeting;
                Ensure(greeting.TextLayout.TextLines.Count == 1 && greeting.Bounds.Width <= sample.Banner.TextArea.Bounds.Width + .5,
                    "Long names are trimmed to one line");
                CheckLayout(sample, "long name at 1040x720", 1040);
            }
            finally { frame.Close(); }
        }
        finally { session.CurrentUser.Name = realName; }
    }

    private static void CheckLayout(MarketplaceView view, string label, double width)
    {
        var banner = view.Banner;
        var size = banner.Bounds.Size;
        var text = Rect(banner.TextArea, banner);
        var illustration = MarketplaceBanner.IllustrationLeft(size);
        var image = MarketplaceBanner.ImageSize;
        var ratio = image.Width / image.Height;
        Ensure(Math.Abs(size.Width / size.Height - ratio) / ratio < .01 && banner.Art.Bounds.Size == size,
            $"{label}: banner shows the whole image at the bitmap ratio ({size.Width:0}x{size.Height:0}, bitmap {image.Width:0}x{image.Height:0})");
        var middle = MarketplaceBanner.CabinetTop(size) / 2;
        Ensure(Math.Abs(text.X - MarketplaceBanner.TextPadding) < .5 && text.Y >= 0 && text.Bottom <= MarketplaceBanner.CabinetTop(size) + .5 &&
            Math.Abs(text.Y + text.Height / 2 - middle) < 1, $"{label}: banner text is centered in the empty top-left area");
        Ensure(text.Right <= size.Width * MarketplaceBanner.TextShare + .5 && text.Right < illustration,
            $"{label}: banner text ({text.Right:0}) stays within 45% and clear of the illustration ({illustration:0})");
        Ensure(banner.Greeting.FontSize >= MarketplaceBanner.MinGreetingSize && banner.Message.FontSize >= MarketplaceBanner.MinMessageSize,
            $"{label}: banner text stays readable ({banner.Greeting.FontSize:0}/{banner.Message.FontSize:0})");
        foreach (var line in new[] { banner.Greeting, banner.Message })
        {
            var box = Rect(line, banner);
            Ensure(box.Right <= text.Right + .5, $"{label}: \"{line.Text}\" fits its block");
            Ensure(!box.Intersects(new Rect(illustration, 0, size.Width - illustration, size.Height)) &&
                !box.Intersects(new Rect(0, MarketplaceBanner.CabinetTop(size), size.Width, size.Height - MarketplaceBanner.CabinetTop(size))),
                $"{label}: \"{line.Text}\" does not cross the illustration");
        }
        Ensure(banner.Greeting.TextLayout.TextLines.Count == 1 && banner.Message.TextLayout.TextLines.Count <= 2, $"{label}: greeting is one line, message at most two");
        var viewport = view.Scroll.Viewport.Width;
        var content = Rect(view.Body, view);
        Ensure(view.Scroll.Extent.Width <= viewport + .5 && content.Right <= viewport + .5, $"{label}: no horizontal scrolling");
        var expected = Math.Min(MarketplaceView.ContentMaxWidth, viewport - 64);
        Ensure(Math.Abs(content.Width - expected) < 1 && Math.Abs(content.X - (viewport - expected) / 2) < 1 && viewport >= width - 20,
            $"{label}: content is {expected:0} wide and centered ({content.Width:0} at {content.X:0})");
        var bannerBox = Rect(banner, view);
        Ensure(Math.Abs(size.Width - expected) < 1 && Math.Abs(bannerBox.X - content.X) < 1 && Math.Abs(bannerBox.Right - content.Right) < 1,
            $"{label}: banner is the content width ({size.Width:0} at {bannerBox.X:0})");
        var row = Rect(view.CollectionRow, view);
        Ensure(Math.Abs(bannerBox.X - row.X) <= 1 && Math.Abs(bannerBox.Right - row.Right) <= 1,
            $"{label}: banner edges ({bannerBox.X:0} to {bannerBox.Right:0}) match the collection row ({row.X:0} to {row.Right:0})");
        var listings = view.ListingsPanel.Children.OfType<Border>().Select(c => Rect(c, view)).ToList();
        Ensure(listings.Count > 0 && listings.All(c => Math.Abs(bannerBox.X - c.X) <= 1 && Math.Abs(bannerBox.Right - c.Right) <= 1),
            $"{label}: banner edges match the listing cards");
        var cards = view.CollectionCards.Values.Select(c => Rect(c, view)).ToList();
        Ensure(cards.All(c => Math.Abs(c.Y - cards[0].Y) < .5 && Math.Abs(c.Width - cards[0].Width) < 1), $"{label}: five equal collection cards in one row");
        Ensure(cards.Zip(cards.Skip(1)).All(p => p.First.Right <= p.Second.X + .5) && cards[^1].Right <= content.Right + .5, $"{label}: collection cards do not overlap or wrap");
        Ensure(view.ListingsPanel.Children.All(c => Rect(c, view).Right <= content.Right + .5), $"{label}: listing cards fit the content width");
    }

    private static void CheckRhythm(Control window, CustomerTopBar topBar, MarketplaceView view, string label)
    {
        var bar = Rect(topBar, window);
        var banner = Rect(view.Banner, window);
        var filters = Rect(view.CollectionRow, window);
        var title = Rect(view.Title, window);
        var first = Rect(view.ListingsPanel.Children[0], window);
        var content = Rect(view.Body, window);
        var gaps = new[] { banner.Y - bar.Bottom, filters.Y - banner.Bottom, title.Y - filters.Bottom };
        Ensure(gaps.All(g => Math.Abs(g - gaps[0]) <= 1 && Math.Abs(g - Ui.Spacing.Section) <= 1),
            $"{label}: navbar-banner, banner-filters and filters-title gaps are {Ui.Spacing.Section:0} ({string.Join(", ", gaps.Select(g => g.ToString("0.#")))})");
        Ensure(Math.Abs(first.Y - title.Bottom - Ui.Spacing.Related) <= 1, $"{label}: title to first card is {Ui.Spacing.Related:0} ({first.Y - title.Bottom:0.#})");
        Ensure(Math.Abs(title.X - content.X) <= 1 && Math.Abs(title.X - banner.X) <= 1, $"{label}: title is left-aligned with the content edge ({title.X:0} vs {content.X:0})");
        Ensure(view.Body.Margin.Bottom == Ui.Spacing.Page && view.ListingsPanel.Spacing == Ui.Spacing.Related, $"{label}: page bottom is {Ui.Spacing.Page:0}");
        Console.WriteLine($"Jelajah rhythm at {label}: gaps {string.Join("/", gaps.Select(g => g.ToString("0.#")))}, title-card {first.Y - title.Bottom:0.#}");
    }

    private static Rect Rect(Visual visual, Visual root) =>
        new(visual.TranslatePoint(default, root) ?? throw new InvalidOperationException("Element is outside Jelajah"), visual.Bounds.Size);

    private static Color ColorOf(IBrush? brush) => brush is ISolidColorBrush solid ? solid.Color : Colors.Transparent;

    private static async Task Pseudo(StyledElement element, string pseudo, bool on)
    { ((IPseudoClasses)element.Classes).Set(pseudo, on); await Task.Delay(100); }

    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static async Task Settle()
    { await Task.Delay(250); await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render); }

    private static void Ensure(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
