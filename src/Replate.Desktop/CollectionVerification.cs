using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Replate.Domain.Entities;

namespace Replate.Desktop;

internal static class CollectionVerification
{
    private static readonly Color Primary = Color.Parse("#245B43");

    internal static async Task Run(MainWindow window)
    {
        var session = window.Session;
        var bar = window.TopBar;
        foreach (var item in session.Cart.Items.ToList()) session.ChangeQuantity(item.ListingId, 0);
        window.ShowMarketplace(); await Settle();
        var all = ListingQuery.Available(session).ToList();

        // Collections
        foreach (var name in MarketplaceView.CollectionNames)
        {
            window.ShowMarketplace(); await Settle();
            var card = window.Marketplace.CollectionCards[name];
            if (name == MarketplaceView.EndingSoon) Press(card, Avalonia.Input.Key.Enter); else Click(card);
            await Settle();
            var page = window.CurrentPage as CollectionView ?? throw new InvalidOperationException($"{name} card opens a collection page");
            Ensure(page.Collection == name && page.Query is null && page.Title.Text == name, $"{name}: page title is the card label");
            CheckPage(window, page, name);
            NoLegacyCategory(page, name);
            var listed = page.VisibleListings;
            Ensure(listed.Count > 0 && page.Feed.Panel.Children.OfType<Border>().Count() == listed.Count, $"{name}: one listing card per listing");
            if (name == MarketplaceView.Cheapest)
                Ensure(listed.Count == all.Count && listed.ToHashSet().SetEquals(all) && Ascending(listed.Select(l => l.RescuePrice)), "Termurah lists every listing by rescue price ascending");
            else if (name == MarketplaceView.EndingSoon)
                Ensure(listed.Count == all.Count && listed.ToHashSet().SetEquals(all) && Ascending(listed.Select(l => l.PickupEnd)), "Segera berakhir (Enter) lists every listing by pickup end ascending");
            else
                Ensure(listed.SequenceEqual(all.Where(l => l.Menu.Category == name)), $"{name}: only that category in the default order");
        }
        window.ShowCollection("Tidak ada");
        Ensure(window.CurrentPage is CollectionView { Collection: MenuCategories.Produce }, "An unknown collection name is ignored");

        // Back link
        var collection = (CollectionView)window.CurrentPage!;
        var link = collection.BackLink;
        Ensure(link.Content is StackPanel linkContent && linkContent.Children.Count == 2 && linkContent.Children[0] == collection.BackGlyph &&
            linkContent.Children[1] is TextBlock { Text: CollectionView.BackLabel } &&
            AutomationProperties.GetName(link) == CollectionView.BackLabel && ColorOf(link.Foreground) == Primary && ColorOf(link.Background) == Colors.Transparent &&
            ColorOf(collection.BackGlyph.Stroke) == Primary && collection.BackGlyph.StrokeThickness == 1.75, "Kembali ke Jelajah is a primary text link with a left arrow");
        await Pseudo(link, ":pointerover", true);
        Ensure(ColorOf(link.Background) == Colors.Transparent && ColorOf(link.Foreground) == Color.Parse("#357F5D") && ColorOf(collection.BackGlyph.Stroke) == Color.Parse("#357F5D"),
            "Back link hover only changes the text and arrow color");
        await Pseudo(link, ":pointerover", false);
        var ring = TopBarVerification.FocusRing(link);
        await Pseudo(link, ":focus-visible", true);
        Ensure(ColorOf(ring.BorderBrush) == Primary && link.Focusable && KeyboardNavigation.GetIsTabStop(link), "Back link is a Tab stop with the primary focus ring");
        await Pseudo(link, ":focus-visible", false);
        Click(link); await Settle();
        Ensure(window.CurrentPage is MarketplaceView && bar.SearchBox.Text == "", "Kembali ke Jelajah returns to Jelajah");

        // Search
        async Task Submit(string text)
        {
            bar.SearchBox.Text = text;
            bar.SearchBox.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Avalonia.Input.Key.Enter, Source = bar.SearchBox });
            await Settle();
        }
        CollectionView Results(string query)
        {
            var page = window.CurrentPage as CollectionView ?? throw new InvalidOperationException($"\"{query}\" opens the results page");
            Ensure(page.Collection is null && page.Query == query.Trim() && page.Title.Text == CollectionView.SearchPrefix + query.Trim() &&
                bar.SearchBox.Text == query, $"\"{query}\": results title and search field keep the keyword");
            Ensure(page.VisibleListings.SequenceEqual(all.Where(l => Matches(l, query))), $"\"{query}\": results match every word in name, description, restaurant or category");
            return page;
        }
        var jelajah = window.CurrentPage;
        await Submit("");
        await Submit("   ");
        Ensure(window.CurrentPage == jelajah, "An empty or blank search does nothing");
        bar.SearchBox.Text = "croissant"; await Settle();
        Ensure(window.CurrentPage == jelajah && window.Marketplace.VisibleCount == all.Count, "Typing alone does not filter Jelajah or change pages");

        await Submit("croissant");
        var results = Results("croissant");
        Ensure(results.VisibleListings.Count > 0 && results.VisibleListings.All(l => l.Menu.Name.Contains("croissant", StringComparison.OrdinalIgnoreCase)), "Search by menu name");
        CheckPage(window, results, "search croissant");
        await Submit("WARUNG sambal");
        results = Results("WARUNG sambal");
        Ensure(results.VisibleListings.Count > 0 && results.VisibleListings.All(l => l.Menu.Restaurant?.Name == "Warung Sambal Ijo") &&
            results.VisibleListings.Count == all.Count(l => l.Menu.Restaurant?.Name == "Warung Sambal Ijo"), "Search by restaurant name, case-insensitive, replaces the results");
        var sample = all.First(l => l.Menu.Name.Split(' ').Length >= 2);
        var reversed = string.Join(" ", sample.Menu.Name.ToUpperInvariant().Split(' ').Reverse());
        await Submit(reversed);
        results = Results(reversed);
        Ensure(results.VisibleListings.Contains(sample), $"Words out of order still match ({reversed})");
        await Submit("croissant sambal");
        Ensure(Results("croissant sambal").VisibleListings.Count == 0, "Every word must match");
        bar.SearchBox.Text = "roti"; Click(bar.SearchButton); await Settle();
        Ensure(Results("roti").VisibleListings.Count > 0 && AutomationProperties.GetName(bar.SearchButton) == "Cari", "The magnifier opens the results");

        await Submit("kata tidak ada");
        results = Results("kata tidak ada");
        var texts = results.Feed.Panel.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
        Ensure(results.VisibleListings.Count == 0 && texts.Contains(CollectionView.SearchEmpty) && texts.Contains("Coba kata kunci lain."), "Unknown words show the empty state");
        var empty = results.Feed.Panel.Children.Single();
        Ensure(Math.Abs(Rect(empty, window).Y - Rect(results.TitleRow, window).Bottom - Ui.Spacing.Related) <= 1, "Empty state sits 16 under the title");
        Click(results.Feed.Panel.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == CollectionView.BackLabel)); await Settle();
        Ensure(window.CurrentPage is MarketplaceView && bar.SearchBox.Text == "", "The empty state button returns to Jelajah and clears the search");

        var longQuery = "nasi " + new string('x', 240);
        await Submit(longQuery);
        results = (CollectionView)window.CurrentPage!;
        Ensure(results.Query == longQuery && results.Title.TextTrimming == TextTrimming.CharacterEllipsis && results.Title.TextLayout.TextLines.Count == 1 &&
            Rect(results.Title, window).Right <= Rect(results.Body, window).Right + .5, "A very long keyword is trimmed with an ellipsis on one line");
        CheckPage(window, results, "long search");

        // Clearing
        await Submit("croissant");
        Click(bar.LogoButton); await Settle();
        Ensure(window.CurrentPage is MarketplaceView && bar.SearchBox.Text == "", "The logo returns to Jelajah and clears the search");
        await Submit("croissant");
        Click(bar.CartButton); await Settle();
        Ensure(window.CurrentPage is CartView && bar.SearchBox.Text == "", "Keranjang clears the search");
        await Submit("croissant");
        Click(bar.OrdersItem); await Settle();
        Ensure(window.CurrentPage is OrdersView && bar.SearchBox.Text == "", "Pesanan saya clears the search");
        bar.SearchBox.Text = "croissant"; await Settle();
        Ensure(window.CurrentPage is OrdersView, "Typing on another page stays on that page");
        bar.SearchBox.Text = "";

        // Cart rule
        window.ShowCollection(MarketplaceView.Cheapest); await Settle();
        var page2 = (CollectionView)window.CurrentPage!;
        var listed2 = page2.VisibleListings;
        var first = listed2.First(l => l.Quantity > 0);
        var other = listed2.First(l => l.Quantity > 0 && l.Menu.RestaurantId != first.Menu.RestaurantId);
        Button Add(SurplusListing listing) => page2.Feed.Panel.Children[page2.VisibleListings.ToList().IndexOf(listing)]
            .GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == "Tambah");
        Click(Add(first)); await Settle();
        Ensure(window.Dialog?.Listing == first && session.Cart.Items.Count == 0, "Tambah on a collection page opens the food dialog");
        Click(window.Dialog!.AddButton); await Settle();
        Ensure(window.Dialog is null && session.Cart.Items.Single().ListingId == first.Id && page2.Feed.Notice.IsVisible && bar.CartBadgeText.Text == "1" &&
            window.CurrentPage == page2, "Tambah ke keranjang on a collection page adds to the cart and stays on the page");
        Click(Add(other)); await Settle();
        Click(window.Dialog!.AddButton); await Settle();
        Ensure(window.Dialog is null && session.Cart.Items.Select(i => i.ListingId).SequenceEqual([first.Id, other.Id]) &&
            session.Cart.CountRestaurants() == 2 && bar.CartBadgeText.Text == "2", "Tambah adds a menu from another restaurant to the same cart");
        window.ShowMarketplace(); await Settle();
        Ensure(session.Cart.Items.Count == 2 && bar.CartBadgeText.Text == "2", "The cart survives navigation");
        session.ChangeQuantity(other.Id, 0); session.ChangeQuantity(first.Id, 0); window.UpdateCartCount();

        // Sizes
        foreach (var size in new[] { new Size(1040, 720), new Size(1920, 1080) })
            foreach (var page in new[] { CollectionView.ForCollection(window, session, "Makanan utama"), CollectionView.ForSearch(window, session, "nasi " + new string('x', 240)) })
            {
                var host = new Border { Child = page, Width = size.Width, Height = size.Height - CustomerTopBar.BarHeight };
                var frame = new Window { Width = 1040, Height = 720, Content = new Canvas { Children = { host } } };
                try
                {
                    frame.Show(); page.Refresh(); await Settle();
                    var label = $"{(page.Collection ?? "search")} at {size.Width:0}x{size.Height:0}";
                    var viewport = page.Scroll.Viewport.Width;
                    var content = Rect(page.Body, page);
                    var expected = Math.Min(MarketplaceView.ContentMaxWidth, viewport - 2 * Ui.Spacing.Page);
                    Ensure(page.Scroll.HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled && page.Scroll.Extent.Width <= viewport + .5 &&
                        content.Right <= viewport + .5 && Math.Abs(content.Width - expected) < 1, $"{label}: no horizontal scrolling, content {content.Width:0} of {expected:0}");
                    Ensure(page.Feed.Panel.Children.All(c => Rect(c, page).Right <= content.Right + .5) && Rect(page.Title, page).Right <= content.Right + .5,
                        $"{label}: title and cards fit the content width");
                }
                finally { frame.Close(); }
            }

        // Category label
        window.ShowMarketplace(); await Settle();
        NoLegacyCategory(window, "Jelajah");
        Ensure(window.Marketplace.CollectionCards.ContainsKey(MenuCategories.Produce) &&
            window.Marketplace.CollectionCards[MenuCategories.Produce].GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == MenuCategories.Produce) &&
            Texts(window).Any(t => t.EndsWith(" · " + MenuCategories.Produce)), "Jelajah shows Buah & sayur on the card and in the listing line");
        await Submit("vegetarian");
        results = Results("vegetarian");
        Ensure(results.VisibleListings.Count > 0 && results.VisibleListings.Count == all.Count(l => l.Menu.Category == MenuCategories.Produce) &&
            results.VisibleListings.All(l => l.Menu.Category == MenuCategories.Produce), "Searching vegetarian still finds Buah & sayur menus");
        NoLegacyCategory(window, "search vegetarian");
        await Submit("buah sayur");
        Ensure(Results("buah sayur").VisibleListings.Count == results.VisibleListings.Count, "Searching the new label finds the same menus");
        var produceListing = all.First(l => l.Menu.Category == MenuCategories.Produce);
        window.ShowFood(produceListing); await Settle();
        Ensure(Texts(window).Contains(MenuCategories.Produce), "Food detail shows Buah & sayur");
        NoLegacyCategory(window, "food detail");
        session.AddToCart(produceListing, 1); window.ShowCart(); await Settle();
        NoLegacyCategory(window, "Keranjang");
        session.ChangeQuantity(produceListing.Id, 0);
        window.ShowCustomerOrders(); await Settle();
        NoLegacyCategory(window, "Pesanan saya");
        var legacyPath = StateVerification.WriteLegacyState();
        var legacyBytes = File.ReadAllBytes(legacyPath);
        var legacySession = new DemoSession(legacyPath);
        Ensure(File.ReadAllBytes(legacyPath).SequenceEqual(legacyBytes), "Loading a legacy state file does not rewrite it");
        legacySession.AsCustomer();
        var legacyWindow = new MainWindow(legacySession) { Width = 1040, Height = 720 };
        try
        {
            legacyWindow.Show(); legacyWindow.ShowMarketplace(); await Settle();
            Ensure(legacySession.StorageError is null && Texts(legacyWindow).Count(t => t.EndsWith(" · " + MenuCategories.Produce)) == 3,
                "A legacy Vegetarian state file shows Buah & sayur on Jelajah");
            NoLegacyCategory(legacyWindow, "legacy Jelajah");
            legacyWindow.ShowCollection(MenuCategories.Produce); await Settle();
            Ensure(legacyWindow.CurrentPage is CollectionView { VisibleListings.Count: 3 }, "Legacy menus fill the Buah & sayur collection");
            NoLegacyCategory(legacyWindow, "legacy collection");
            foreach (var menu in legacySession.Restaurants.SelectMany(r => r.Menus).Where(m => m.Category == MenuCategories.Produce)) menu.Deactivate();
            legacyWindow.ShowCollection(MenuCategories.Produce); await Settle();
            var emptyPage = (CollectionView)legacyWindow.CurrentPage!;
            Ensure(emptyPage.VisibleListings.Count == 0 && Texts(emptyPage).Contains(CollectionView.CollectionEmpty) && emptyPage.Intro is { IsVisible: false } &&
                !Texts(emptyPage).Contains(CollectionView.Intros[MenuCategories.Produce]), "An empty collection shows the empty state without the intro");
            CheckPage(legacyWindow, emptyPage, "empty collection");
        }
        finally { legacyWindow.Close(); }

        // Guards
        var ownerSession = new DemoSession().AsOwner();
        var ownerWindow = new MainWindow(ownerSession);
        ownerWindow.ShowCollection(MarketplaceView.Cheapest);
        Ensure(ownerWindow.CurrentPage is SignInView && !ownerSession.IsSignedIn, "A restaurant owner opening a collection is signed out");
        ownerSession = new DemoSession().AsOwner();
        ownerWindow = new MainWindow(ownerSession);
        ownerWindow.ShowSearch("croissant");
        Ensure(ownerWindow.CurrentPage is SignInView && !ownerSession.IsSignedIn, "A restaurant owner opening search results is signed out");
        window.ShowMarketplace(); await Settle();
        Console.WriteLine("All Replate collection and search checks passed (five collections, ordering, categories, search words, empty states, clearing, cart rule, 1040x720 and 1920x1080, role guards).");
    }

    private static void CheckPage(MainWindow window, CollectionView page, string label)
    {
        var bar = Rect(window.TopBar, window);
        var link = Rect(page.BackLink, window);
        var glyph = Rect(page.BackGlyph, window);
        var title = Rect(page.Title, window);
        var row = Rect(page.TitleRow, window);
        var body = Rect(page.Body, window);
        var header = Rect(page.Header, window);
        var panel = page.Feed.Panel;
        var label2 = Rect(page.BackLink.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == CollectionView.BackLabel), window);
        Ensure(page.BackGlyph.Bounds.Width <= CollectionView.BackIconSize && page.BackGlyph.Bounds.Height <= CollectionView.BackIconSize && page.BackGlyph.StrokeThickness == 1.75 &&
            page.BackGlyph.StrokeLineCap == PenLineCap.Round && link.Height >= CollectionView.BackLinkMinHeight - .5 && Math.Abs(label2.X - glyph.Right - 8) <= 1 &&
            Math.Abs(glyph.Center.Y - label2.Center.Y) <= 1 && Math.Abs(glyph.Center.Y - link.Center.Y) <= 1,
            $"{label}: back arrow {page.BackGlyph.Bounds.Width:0}x{page.BackGlyph.Bounds.Height:0}, 8 from the text, centered; link {link.Height:0.#} high");
        var hasIntro = page.Collection is not null && page.VisibleListings.Count > 0;
        if (hasIntro)
        {
            var intro = page.Intro ?? throw new InvalidOperationException($"{label}: collection has an intro");
            var introBox = Rect(intro, window);
            Ensure(intro.IsVisible && intro.Text == CollectionView.Intros[page.Collection!] && intro.FontSize < page.Title.FontSize && intro.FontSize == CollectionView.IntroSize &&
                ColorOf(intro.Foreground) == Color.Parse("#59675D") && intro.TextWrapping == TextWrapping.Wrap && page.Header.Children[1] == intro &&
                Math.Abs(introBox.Y - row.Bottom - CollectionView.IntroGap) <= 1 && Math.Abs(introBox.Bottom - header.Bottom) <= .5,
                $"{label}: intro \"{intro.Text}\" sits 8 under the title in muted {intro.FontSize:0}");
        }
        else Ensure(page.Intro is null or { IsVisible: false } && Math.Abs(header.Bottom - row.Bottom) <= .5, $"{label}: no intro on search results or empty states");
        Ensure(window.TopBar.IsEffectivelyVisible && !page.GetVisualDescendants().OfType<MarketplaceBanner>().Any() &&
            page.Body.Children.Count == 3 && page.Body.Children[0] == page.BackLink && page.Body.Children[1] == page.Header && page.Body.Children[2] == panel &&
            page.Header.Children[0] == page.TitleRow && page.Header.Spacing == CollectionView.IntroGap,
            $"{label}: top bar, then back link, title and listing cards, no banner");
        Ensure(page.Title.FontSize == MarketplaceView.TitleSize && page.Title.FontWeight == FontWeight.Bold && ColorOf(page.Title.Foreground) == Color.Parse("#23382C") &&
            page.Title.TextLayout.TextLines.Count == 1, $"{label}: title is {MarketplaceView.TitleSize:0} bold ink on one line");
        Ensure(Math.Abs(link.Y - bar.Bottom - Ui.Spacing.Section) <= 1 && Math.Abs(row.Y - link.Bottom - Ui.Spacing.Related) <= 1 &&
            (panel.Children.Count == 0 || Math.Abs(Rect(panel.Children[0], window).Y - header.Bottom - Ui.Spacing.Related) <= 1) && panel.Spacing == Ui.Spacing.Related &&
            page.Body.Margin.Bottom == Ui.Spacing.Page, $"{label}: rhythm 24 to the back link, 16 to the title, 16 to the first card ({link.Y - bar.Bottom:0.#}/{row.Y - link.Bottom:0.#})");
        Ensure(Math.Abs(title.X - body.X) <= 1 && Math.Abs(glyph.X - body.X) <= 1, $"{label}: back arrow and title align with the content edge ({glyph.X:0}, {title.X:0}, {body.X:0})");
        Ensure(page.Scroll.HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled && page.Scroll.Extent.Width <= page.Scroll.Viewport.Width + .5,
            $"{label}: no horizontal scrolling");
    }

    private static bool Matches(SurplusListing listing, string query) =>
        query.Split(' ', StringSplitOptions.RemoveEmptyEntries).All(word =>
            listing.Menu.Name.Contains(word, StringComparison.OrdinalIgnoreCase) || listing.Menu.Description.Contains(word, StringComparison.OrdinalIgnoreCase) ||
            listing.Menu.Restaurant?.Name.Contains(word, StringComparison.OrdinalIgnoreCase) == true || listing.Menu.Category.Contains(word, StringComparison.OrdinalIgnoreCase) ||
            (listing.Menu.Category == MenuCategories.Produce && "vegetarian".Contains(word, StringComparison.OrdinalIgnoreCase)));

    internal static void NoLegacyCategory(Visual root, string label)
    {
        var texts = Texts(root);
        Ensure(!texts.Any(t => t.Contains(MenuCategories.LegacyProduce, StringComparison.Ordinal)) &&
            !root.GetVisualDescendants().OfType<Control>().Any(c => AutomationProperties.GetName(c)?.Contains(MenuCategories.LegacyProduce, StringComparison.Ordinal) == true),
            $"{label}: the old Vegetarian label is gone");
    }

    private static List<string> Texts(Visual root) =>
        root.GetVisualDescendants().OfType<TextBlock>().Where(t => t.IsEffectivelyVisible).Select(t => t.Text ?? "").ToList();

    private static bool Ascending<T>(IEnumerable<T> values) where T : IComparable<T> =>
        values.Zip(values.Skip(1)).All(p => p.First.CompareTo(p.Second) <= 0);

    private static Rect Rect(Visual visual, Visual root) =>
        new(visual.TranslatePoint(default, root) ?? throw new InvalidOperationException("Element is outside the page"), visual.Bounds.Size);

    private static Color ColorOf(IBrush? brush) => brush is ISolidColorBrush solid ? solid.Color : Colors.Transparent;

    private static async Task Pseudo(StyledElement element, string pseudo, bool on)
    { ((IPseudoClasses)element.Classes).Set(pseudo, on); await Task.Delay(100); }

    private static void Press(Control target, Key key) =>
        target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key, Source = target });

    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static async Task Settle()
    { await Task.Delay(250); await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render); }

    private static void Ensure(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
