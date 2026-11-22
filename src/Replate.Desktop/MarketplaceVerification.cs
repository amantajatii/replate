using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
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
    private static readonly Color Primary = Color.Parse("#1F6B4F");
    private static readonly Color Ink = Color.Parse("#202522");
    private static readonly Color CanvasColor = ((ISolidColorBrush)Ui.Canvas).Color;
    private static readonly Color OnPrimary = Colors.White;
    private static readonly Color Selection = Color.Parse("#DDEDE5");
    private static readonly Color Divider = Color.Parse("#D7E4DF");

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
            ColorOf(view.Title.Foreground) == Color.Parse("#202522") && view.Title.HorizontalAlignment == Avalonia.Layout.HorizontalAlignment.Left,
            $"Jelajahi Menu is a bold, left-aligned section title in ink ({view.Title.FontSize:0})");
        Ensure(!view.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text is "Semua restoran" or "Makanan tersedia" || t.Text?.Contains('—') == true),
            "Decorative section titles are removed and no text uses an em dash");

        // Filter cards
        Ensure(view.CollectionCards.Keys.SequenceEqual(MarketplaceView.CollectionNames) && !view.CollectionCards.ContainsKey("Semua"), "Five collection cards without Semua");
        foreach (var (label, card) in view.CollectionCards)
        {
            var icon = card.GetVisualDescendants().OfType<Path>().Single();
            var frame = Face(card);
            var text = card.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == label);
            Ensure(icon.StrokeThickness == 1.75 && icon.StrokeLineCap == PenLineCap.Round && icon.Width == 24 && ColorOf(icon.Stroke) == OnPrimary &&
                TopBarVerification.Contrast(OnPrimary, Primary) >= 3, $"{label}: white 24 line icon ({TopBarVerification.Contrast(OnPrimary, Primary):0.0}:1)");
            Ensure(text.FontSize == 14 && ColorOf(text.Foreground) == OnPrimary && ColorOf(card.Foreground) == OnPrimary && TopBarVerification.Contrast(OnPrimary, Primary) >= 4.5,
                $"{label}: white 14 label under the icon ({TopBarVerification.Contrast(OnPrimary, Primary):0.0}:1)");
            var shadow = frame.BoxShadow;
            Ensure(frame.CornerRadius == new CornerRadius(16) && shadow.ToString() == MarketplaceView.PrimaryCardShadow.ToString() && shadow.Count == 1 &&
                IsPrimaryTint(shadow[0].Color) && shadow[0].OffsetY == 6 && shadow[0].Blur == 18 && Math.Abs(shadow[0].Color.A / 255d - .30) < .01 && card.Height == 96 &&
                ColorOf(card.Background) == Primary && ColorOf(frame.Background) == Primary && ColorOf(card.BorderBrush) == Colors.Transparent && !card.ClipToBounds,
                $"{label}: primary card with the primary-tinted card shadow ({shadow}) and no border");
            Ensure(card.Focusable && KeyboardNavigation.GetIsTabStop(card), $"{label}: reachable with Tab");
            var ring = TopBarVerification.FocusRing(card);
            Ensure(card.FocusAdorner == Ui.NoFocusAdorner && ColorOf(ring.BorderBrush) == Colors.Transparent, $"{label}: no default focus adorner");
            await Pseudo(card, ":focus-visible", true);
            Ensure(ColorOf(ring.BorderBrush) == Primary && ring.BorderThickness == new Thickness(Ui.FocusRingWidth) && ColorOf(card.BorderBrush) == Colors.Transparent &&
                ring.Margin == new Thickness(-(Ui.FocusRingWidth + Ui.FocusRingOffset)) && TopBarVerification.Contrast(Primary, CanvasColor) >= 3,
                $"{label}: keyboard focus shows a 2 px primary ring outside the card on the canvas ({TopBarVerification.Contrast(Primary, CanvasColor):0.0}:1)");
            await Pseudo(card, ":focus-visible", false);
        }
        await CheckHover(view, MarketplaceView.Cheapest, "Jelajah");
        await CheckHover(view, MarketplaceView.CollectionNames[^1], "Jelajah");

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
            host.Styles.Add(Ui.FocusStyles());
            var frame = new Window { Width = 1040, Height = 720, Content = new Canvas { Children = { host } } };
            try
            {
                frame.Show(); sample.Refresh(); await Settle();
                CheckLayout(sample, $"{size.Width:0}x{size.Height:0}", size.Width);
                if (size.Width != 1360)
                    foreach (var card in new[] { MarketplaceView.Cheapest, MarketplaceView.CollectionNames[^1] })
                        await CheckHover(sample, card, $"{size.Width:0}x{size.Height:0}");
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
        var cardWidth = (row.Width - 4 * Ui.Spacing.Related) / 5;
        Ensure(view.CollectionRow.ColumnSpacing == Ui.Spacing.Related && cards.All(c => Math.Abs(c.Height - 96) < .5 && Math.Abs(c.Width - cardWidth) < 1) &&
            cards.Zip(cards.Skip(1)).All(p => Math.Abs(p.Second.X - p.First.Right - Ui.Spacing.Related) < 1),
            $"{label}: collection cards keep height 96, width {cardWidth:0.#} and the 16 gap ({cards[0].Width:0.#}x{cards[0].Height:0.#})");
        Ensure(cards.Zip(cards.Skip(1)).All(p => p.First.Right <= p.Second.X + .5) && cards[^1].Right <= content.Right + .5, $"{label}: collection cards do not overlap or wrap");
        Ensure(view.ListingsPanel.Children.All(c => Rect(c, view).Right <= content.Right + .5), $"{label}: listing cards fit the content width");
    }

    private static async Task CheckHover(MarketplaceView view, string name, string label)
    {
        var card = view.CollectionCards[name];
        var face = Face(card);
        var icon = card.GetVisualDescendants().OfType<Path>().Single();
        var text = card.GetVisualDescendants().OfType<TextBlock>().Single();
        var others = view.CollectionCards.Values.Where(c => c != card).ToList();
        var before = others.Select(c => Rect(c, view)).ToList();
        var bounds = card.Bounds;
        var duration = MarketplaceView.HoverDuration;
        Ensure(duration == TimeSpan.FromMilliseconds(120) && card.Transitions is { Count: 3 } transitions &&
            transitions.OfType<BrushTransition>().Select(t => t.Property).ToHashSet().SetEquals([TemplatedControl.BackgroundProperty, TemplatedControl.ForegroundProperty]) &&
            transitions.OfType<TransformOperationsTransition>().Single().Property == Visual.RenderTransformProperty &&
            face.Transitions is { Count: 1 } faceTransitions && faceTransitions.OfType<BoxShadowsTransition>().Single().Property == Border.BoxShadowProperty &&
            transitions.Concat(faceTransitions).All(t => t is TransitionBase { Easing: CubicEaseOut } b && b.Duration == duration) &&
            card.RenderTransformOrigin == RelativePoint.Center, $"{label} {name}: color, scale and shadow use one 120 ms ease-out transition from the center");

        await Hold(card, ":pointerover", true);
        var fill = ColorOf(card.Background);
        Ensure(fill == Selection && fill != Primary && ColorOf(face.Background) == Selection && ColorOf(text.Foreground) == Primary && ColorOf(icon.Stroke) == Primary &&
            TopBarVerification.Contrast(Primary, fill) >= 4.5,
            $"{label} {name}: hover uses the selection fill with primary icon and label ({TopBarVerification.Contrast(Primary, fill):0.0}:1)");
        var matrix = card.RenderTransform!.Value;
        Ensure(Math.Abs(matrix.M11 - MarketplaceView.HoverScale) <= .005 && Math.Abs(matrix.M22 - MarketplaceView.HoverScale) <= .005 &&
            Math.Abs(MarketplaceView.HoverScale - 1.03) <= .005, $"{label} {name}: hover scales to 1.03 ({matrix.M11:0.###})");
        Ensure(card.Bounds == bounds && others.Select(c => Rect(c, view)).Zip(before).All(p => p.First == p.Second),
            $"{label} {name}: hover leaves the position and size of every other card unchanged");
        Ensure(card.ZIndex > 0 && others.All(c => c.ZIndex < card.ZIndex), $"{label} {name}: the hovered card sits above its neighbours");
        var hover = face.BoxShadow;
        Ensure(hover.ToString() == MarketplaceView.PrimaryCardHoverShadow.ToString() && hover.Count == 1 && IsPrimaryTint(hover[0].Color) &&
            hover[0].OffsetY == 10 && hover[0].Blur == 24 && Math.Abs(hover[0].Color.A / 255d - .35) < .01 &&
            hover[0].Color.A > MarketplaceView.PrimaryCardShadow[0].Color.A, $"{label} {name}: hover deepens the primary-tinted shadow ({hover})");
        var scaled = Scaled(card, view.Scroll);
        var viewport = new Rect(view.Scroll.Viewport);
        var clips = card.GetVisualAncestors().TakeWhile(a => a != view.Scroll && a is not ScrollContentPresenter).Where(a => a.ClipToBounds).ToList();
        Ensure(scaled.Width > bounds.Width && viewport.Contains(scaled) && viewport.Contains(scaled.Inflate(new Thickness(hover[0].Blur, 0))) && clips.Count == 0,
            $"{label} {name}: the hovered card and its shadow are not clipped ({scaled.X:0.#} to {scaled.Right:0.#} within {viewport.Width:0})");
        Ensure(others.All(o => !Scaled(o, view.Scroll).Intersects(scaled)), $"{label} {name}: the hovered card does not cover its neighbours");
        Ensure(view.Scroll.Extent.Width <= view.Scroll.Viewport.Width + .5, $"{label} {name}: no horizontal scrolling while hovered");
        var ring = TopBarVerification.FocusRing(card);
        await Pseudo(card, ":focus-visible", true);
        Ensure(ColorOf(ring.BorderBrush) == Primary && ring.IsVisible && ring.GetVisualAncestors().Contains(card) &&
            ring.Parent is Panel panel && panel.Children.IndexOf(ring) > panel.Children.IndexOf(face), $"{label} {name}: the focus ring stays on top while hovered");
        await Pseudo(card, ":focus-visible", false);
        await Hold(card, ":pressed", true);
        var pressed = ColorOf(card.Background);
        Ensure(pressed == Divider && pressed != Selection && pressed != Primary && ColorOf(text.Foreground) == Primary && ColorOf(icon.Stroke) == Primary &&
            TopBarVerification.Contrast(Primary, pressed) >= 4.5, $"{label} {name}: press uses the divider fill, one step under hover ({TopBarVerification.Contrast(Primary, pressed):0.0}:1)");
        await Hold(card, ":pressed", false);
        await Hold(card, ":pointerover", false);
        Ensure(ColorOf(card.Background) == Primary && ColorOf(text.Foreground) == OnPrimary && ColorOf(icon.Stroke) == OnPrimary &&
            Math.Abs(card.RenderTransform!.Value.M11 - 1) <= .005 && card.ZIndex == 0 && face.BoxShadow.ToString() == MarketplaceView.PrimaryCardShadow.ToString(),
            $"{label} {name}: leaving returns to primary, white, scale 1 and the resting shadow");
    }

    private static Rect Scaled(Visual visual, Visual root) =>
        visual.TransformToVisual(root) is { } matrix ? new Rect(visual.Bounds.Size).TransformToAABB(matrix) : throw new InvalidOperationException("Element is outside Jelajah");

    private static Border Face(Button card) => card.GetVisualDescendants().OfType<Border>().Single(b => b.Name == MarketplaceView.CardFacePart);

    private static bool IsPrimaryTint(Color color) => color.R == Primary.R && color.G == Primary.G && color.B == Primary.B && color.A < 255;

    private static async Task Hold(StyledElement element, string pseudo, bool on)
    { ((IPseudoClasses)element.Classes).Set(pseudo, on); await Task.Delay(MarketplaceView.HoverDuration * 3); }

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
