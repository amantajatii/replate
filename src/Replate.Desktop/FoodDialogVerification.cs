using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Replate.Domain.Entities;

namespace Replate.Desktop;

internal static class FoodDialogVerification
{
    private static readonly Color Primary = Color.Parse("#245B43");
    private static readonly Color Selection = Color.Parse("#E0E9DF");
    private static readonly Color Divider = Color.Parse("#DDE3DA");
    private static readonly Color Muted = Color.Parse("#59675D");

    internal static async Task Run(MainWindow window)
    {
        var session = window.Session;
        var bar = window.TopBar;
        foreach (var item in session.Cart.Items.ToList()) session.ChangeQuantity(item.ListingId, 0);
        window.ShowMarketplace(); await Settle();
        var feed = window.Marketplace.Feed;
        var listing = feed.Listings.First(l => l.Quantity >= 3);
        var store = session.StorePath!;
        var stored = File.ReadAllText(store);
        var storedAt = File.GetLastWriteTimeUtc(store);

        // Opening
        Click(feed.AddButton(listing)); await Settle();
        var dialog = window.Dialog ?? throw new InvalidOperationException("Tambah opens the food dialog");
        Ensure(dialog.Listing == listing && session.Cart.Items.Count == 0 && !bar.CartBadge.IsVisible && window.CurrentPage == window.Marketplace,
            "Tambah opens the dialog without adding to the cart or leaving Jelajah");
        Ensure(window.OwnedWindows.Count == 0 && TopLevel.GetTopLevel(dialog) == window, "The dialog is an overlay inside the window, not an OS window");
        Ensure(window.GetVisualDescendants().OfType<FoodDialog>().Count() == 1, "One dialog at a time");

        // Content
        var menu = listing.Menu;
        Ensure(dialog.Photo.Width == FoodDialog.PhotoSize && dialog.Photo.Height == FoodDialog.PhotoSize &&
            (dialog.Photo.HasPhoto || dialog.Photo.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Foto belum tersedia" && t.FontSize == 14)),
            "Large photo or the large Foto belum tersedia placeholder");
        Ensure(dialog.NameText.Text == menu.Name && dialog.NameText.FontWeight >= FontWeight.Bold && dialog.MetaText.Text == $"{menu.Restaurant?.Name} · {menu.Category}" &&
            dialog.DescriptionText.Text == menu.Description, "Name, restaurant, category and description");
        Ensure(dialog.PickupText.Text == $"Pengambilan {listing.PickupStart.ToLocalTime():HH:mm}–{listing.PickupEnd.ToLocalTime():HH:mm}" &&
            dialog.StockText.Text == $"{listing.Quantity} porsi tersisa", "Pickup window and remaining portions");
        Ensure(dialog.PriceText.Text == DemoSession.Rupiah(listing.RescuePrice) && dialog.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "/porsi") &&
            dialog.NormalPriceText.Text == DemoSession.Rupiah(menu.NormalPrice) && dialog.NormalPriceText.TextDecorations == TextDecorations.Strikethrough &&
            dialog.SavingText.Text == $"Hemat {listing.CalculateDiscount():0}%", "Rescue price per portion, struck normal price and savings");
        Ensure(!dialog.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text?.Contains('—') == true), "No em dash in the dialog");

        // Style
        Ensure(ColorOf(dialog.ScrimLayer.Background) == Color.Parse("#6623382C"), "Scrim is ink at 40% alpha");
        Ensure(dialog.Card.Width == FoodDialog.CardWidth && dialog.Card.CornerRadius == new CornerRadius(16) && dialog.Card.BorderThickness == default &&
            dialog.Card.BoxShadow.ToString() == MarketplaceView.CardShadow.ToString() && ColorOf(dialog.Card.Background) == Colors.White,
            "White card, card radius and shadow, no border");
        Ensure(ColorOf(dialog.Bar.Background) == Selection && ColorOf(dialog.AddButton.Background) == Primary && ColorOf(dialog.AddButton.Foreground) == Colors.White &&
            dialog.AddButton.Content as string == FoodDialog.AddLabel, "Selection bar with the primary Tambah ke keranjang button");
        foreach (var stepper in new[] { dialog.MinusButton, dialog.PlusButton })
            Ensure(stepper.CornerRadius == new CornerRadius(9999) && stepper.Width == stepper.Height && stepper.Content is PathIcon, "Round stepper buttons");
        Ensure(ColorOf(dialog.PlusButton.Background) == Primary && ColorOf(dialog.PlusButton.Foreground) == Colors.White &&
            ColorOf(dialog.MinusButton.Background) == Divider && ColorOf(dialog.MinusButton.Foreground) == Muted,
            "Enabled steppers are primary, disabled ones use the divider fill with a muted icon");
        Ensure(dialog.CloseButton.FocusAdorner == Ui.NoFocusAdorner && ColorOf(TopBarVerification.FocusRing(dialog.CloseButton).BorderBrush) == Colors.Transparent,
            "Dialog buttons use the shared focus style");

        // Focus
        Ensure(window.FocusManager?.GetFocusedElement() == dialog.CloseButton && !TopBarVerification.FocusVisible(dialog.CloseButton),
            "Focus moves into the dialog, without a ring after a click");
        Ensure(KeyboardNavigation.GetTabNavigation(dialog.Card) == KeyboardNavigationMode.Cycle, "The dialog card cycles Tab");
        dialog.AddButton.Focus(NavigationMethod.Tab);
        for (var i = 0; i < 6; i++)
        {
            Key((Control)window.FocusManager!.GetFocusedElement()!, Avalonia.Input.Key.Tab); await Settle();
            Ensure(Inside(dialog, window.FocusManager.GetFocusedElement()) && (i > 0 || window.FocusManager.GetFocusedElement() != dialog.AddButton),
                $"Tab moves and stays inside the dialog ({window.FocusManager.GetFocusedElement()})");
        }
        dialog.CloseButton.Focus(NavigationMethod.Tab);
        for (var i = 0; i < 6; i++)
        {
            Key((Control)window.FocusManager!.GetFocusedElement()!, Avalonia.Input.Key.Tab, KeyModifiers.Shift); await Settle();
            Ensure(Inside(dialog, window.FocusManager.GetFocusedElement()), "Shift+Tab stays inside the dialog");
        }
        dialog.CloseButton.Focus(NavigationMethod.Pointer);

        // Quantity
        Ensure(dialog.Quantity == 1 && dialog.QuantityText.Text == "1" && !dialog.MinusButton.IsEnabled && dialog.PlusButton.IsEnabled &&
            dialog.TotalText.Text == DemoSession.Rupiah(listing.RescuePrice), "Quantity starts at 1 with the rescue price as total");
        Click(dialog.PlusButton); Click(dialog.PlusButton);
        Ensure(dialog.Quantity == 3 && dialog.QuantityText.Text == "3" && dialog.TotalText.Text == DemoSession.Rupiah(listing.RescuePrice * 3) && dialog.MinusButton.IsEnabled,
            "Plus raises the quantity and the total");
        Click(dialog.MinusButton);
        Ensure(dialog.Quantity == 2 && dialog.TotalText.Text == DemoSession.Rupiah(listing.RescuePrice * 2), "Minus lowers the quantity and the total");

        // Dismissal
        Key(dialog.CloseButton, Avalonia.Input.Key.Escape); await Settle();
        Ensure(window.Dialog is null && session.Cart.Items.Count == 0 && window.FocusManager?.GetFocusedElement() == feed.AddButton(listing),
            "Esc closes without adding and returns focus to Tambah");
        Click(feed.AddButton(listing)); await Settle();
        Click(window.Dialog!.CloseButton); await Settle();
        Ensure(window.Dialog is null && session.Cart.Items.Count == 0, "X closes without adding");
        Click(feed.DetailsButton(listing)); await Settle();
        dialog = window.Dialog ?? throw new InvalidOperationException("The menu name opens the dialog");
        await Settle();
        var corner = new Point(8, window.Bounds.Height - 8);
        Ensure(window.InputHitTest(corner) is Visual hit && (hit == dialog.ScrimLayer || dialog.ScrimLayer.IsVisualAncestorOf(hit)), "The dimmed area covers the window");
        dialog.ScrimLayer.RaiseEvent(new PointerPressedEventArgs(dialog.ScrimLayer, new Pointer(124, PointerType.Mouse, true), window, corner, 0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));
        await Settle();
        Ensure(window.Dialog is null && session.Cart.Items.Count == 0 && window.FocusManager?.GetFocusedElement() == feed.DetailsButton(listing),
            "A click on the dimmed area closes without adding and returns focus to the name");
        Ensure(File.ReadAllText(store) == stored && File.GetLastWriteTimeUtc(store) == storedAt, "Opening and closing the dialog writes no state");

        // Keyboard
        await Pseudo(feed.AddButton(listing), ":focus-visible", true);
        Click(feed.AddButton(listing)); await Settle();
        Ensure(window.FocusManager?.GetFocusedElement() == window.Dialog!.CloseButton && TopBarVerification.FocusVisible(window.Dialog.CloseButton),
            "Opened from the keyboard, focus shows the ring inside the dialog");
        Key(window.Dialog.CloseButton, Avalonia.Input.Key.Escape); await Settle();
        Ensure(window.FocusManager?.GetFocusedElement() == feed.AddButton(listing) && TopBarVerification.FocusVisible(feed.AddButton(listing)),
            "Closing returns keyboard focus to Tambah");
        await Pseudo(feed.AddButton(listing), ":focus-visible", false);

        // Card tap
        var photo = feed.Panel.Children.OfType<Border>().ElementAt(feed.Listings.ToList().IndexOf(listing)).GetVisualDescendants().OfType<MenuPhotoView>().Single();
        photo.RaiseEvent(new TappedEventArgs(InputElement.TappedEvent, new PointerEventArgs(InputElement.PointerReleasedEvent, photo,
            new Pointer(125, PointerType.Mouse, true), window, default, 0, new PointerPointProperties(), KeyModifiers.None)));
        await Settle();
        Ensure(window.Dialog?.Listing == listing && session.Cart.Items.Count == 0, "A click on the photo opens the same dialog");
        window.Dialog!.Close(); await Settle();

        // Adding
        Click(feed.AddButton(listing)); await Settle();
        dialog = window.Dialog!;
        Click(dialog.PlusButton);
        Click(dialog.AddButton); await Settle();
        Ensure(window.Dialog is null && session.Cart.Items.Single().ListingId == listing.Id && session.Cart.Items.Single().Quantity == 2 &&
            bar.CartBadgeText.Text == "2" && feed.Notice.IsVisible && feed.Notice.Text == $"{menu.Name} ditambahkan ke keranjang." &&
            window.CurrentPage == window.Marketplace, "Tambah ke keranjang adds the chosen quantity, closes, updates the badge and stays on Jelajah");
        Ensure(window.FocusManager?.GetFocusedElement() == feed.AddButton(listing), "Focus returns to the rebuilt Tambah button");

        // Limit
        Click(feed.AddButton(listing)); await Settle();
        dialog = window.Dialog!;
        var limit = listing.Quantity - 2;
        Ensure(dialog.Limit == limit, "The limit is the stock minus the cart quantity");
        for (var i = 0; i < listing.Quantity + 2; i++) if (dialog.PlusButton.IsEnabled) Click(dialog.PlusButton);
        Ensure(dialog.Quantity == limit && !dialog.PlusButton.IsEnabled && dialog.LimitNote.IsVisible && dialog.LimitNote.Text == $"Maksimal {limit} porsi" &&
            dialog.TotalText.Text == DemoSession.Rupiah(listing.RescuePrice * limit), "Plus stops at the limit with Maksimal N porsi");
        Click(dialog.AddButton); await Settle();
        Ensure(window.Dialog is null && session.Cart.Items.Single().Quantity == listing.Quantity && bar.CartBadgeText.Text == listing.Quantity.ToString(),
            "Adding to the limit fills the cart to the stock");
        Click(feed.AddButton(listing)); await Settle();
        dialog = window.Dialog!;
        Ensure(dialog.Limit == 0 && !dialog.AddButton.IsEnabled && !dialog.PlusButton.IsEnabled && dialog.LimitNote.Text == FoodDialog.FullText,
            "A full cart disables Tambah with Sudah di keranjang dengan jumlah maksimal");
        Ensure(ColorOf(dialog.AddButton.Background) == Divider && ColorOf(dialog.AddButton.Foreground) == Muted, "Disabled Tambah ke keranjang looks disabled");
        dialog.Close(); await Settle();

        // One restaurant
        var other = feed.Listings.First(l => l.Menu.RestaurantId != listing.Menu.RestaurantId);
        Click(feed.AddButton(other)); await Settle();
        dialog = window.Dialog!;
        Click(dialog.AddButton); await Settle();
        Ensure(window.Dialog == dialog && dialog.ErrorText.IsVisible && dialog.ErrorText.Text == ListingFeed.OneRestaurantMessage &&
            session.Cart.Items.Single().ListingId == listing.Id, "One restaurant per cart shows the message inside the open dialog without adding");
        dialog.Close(); await Settle();
        session.ChangeQuantity(listing.Id, 0); window.UpdateCartCount();

        // Unavailable
        Click(feed.AddButton(other)); await Settle();
        dialog = window.Dialog!;
        var end = other.PickupEnd;
        other.PickupEnd = DateTime.UtcNow.AddMinutes(-1);
        try
        {
            dialog.Update();
            Ensure(!dialog.AddButton.IsEnabled && !dialog.PlusButton.IsEnabled && dialog.LimitNote.Text == FoodDialog.UnavailableText,
                "An expired offer disables Tambah with Penawaran tidak tersedia");
            dialog.Submit();
            Ensure(session.Cart.Items.Count == 0 && window.Dialog == dialog, "An expired offer is never added");
        }
        finally { other.PickupEnd = end; }
        dialog.Close(); await Settle();

        // Pages
        window.ShowCollection(MarketplaceView.Cheapest); await Settle();
        var collection = (CollectionView)window.CurrentPage!;
        Click(collection.Feed.AddButton(collection.VisibleListings[0])); await Settle();
        Ensure(window.Dialog?.Listing == collection.VisibleListings[0] && session.Cart.Items.Count == 0, "Tambah on a collection page opens the dialog");
        window.Dialog!.Close(); await Settle();
        window.ShowSearch("croissant"); await Settle();
        var results = (CollectionView)window.CurrentPage!;
        Click(results.Feed.DetailsButton(results.VisibleListings[0])); await Settle();
        Ensure(window.Dialog?.Listing == results.VisibleListings[0] && session.Cart.Items.Count == 0, "Search results open the dialog");
        window.Dialog!.Close(); await Settle();

        // Sizes
        var (width, height) = (window.Width, window.Height);
        foreach (var size in new[] { new Size(1040, 720), new Size(1920, 1080) })
        {
            window.Width = size.Width; window.Height = size.Height;
            window.ShowMarketplace(); await Settle();
            feed = window.Marketplace.Feed;
            Click(feed.AddButton(feed.Listings[0])); await Settle();
            dialog = window.Dialog!;
            var card = Rect(dialog.Card, window);
            var bounds = window.Bounds;
            Ensure(card.X >= FoodDialog.Inset - .5 && card.Y >= FoodDialog.Inset - .5 && card.Right <= bounds.Width - FoodDialog.Inset + .5 &&
                card.Bottom <= bounds.Height - FoodDialog.Inset + .5, $"{size.Width:0}x{size.Height:0}: the dialog fits inside the window ({card})");
            Ensure(Math.Abs(card.X - (bounds.Width - card.Right)) < 1 && Math.Abs(card.Y - (bounds.Height - card.Bottom)) < 1,
                $"{size.Width:0}x{size.Height:0}: the dialog is centered");
            Ensure(Rect(dialog.Bar, window).Bottom <= card.Bottom + .5 && Rect(dialog.AddButton, window).Right <= card.Right + .5, $"{size.Width:0}x{size.Height:0}: the bar is not cut off");
            Ensure(dialog.Scroll.HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled, "No horizontal scrolling in the dialog");
            dialog.Close(); await Settle();
        }
        window.Width = width; window.Height = height;
        window.ShowMarketplace(); await Settle();
        foreach (var size in new[] { new Size(1040, 720), new Size(1920, 1080) })
        {
            var sample = new FoodDialog(session, listing, (_, _) => null, false);
            var host = new Border { Child = sample, Width = size.Width, Height = size.Height };
            var frame = new Window { Width = 1040, Height = 720, Content = new Canvas { Children = { host } } };
            try
            {
                frame.Show(); await Settle();
                var card = Rect(sample.Card, host);
                Ensure(card.X >= FoodDialog.Inset - .5 && card.Y >= FoodDialog.Inset - .5 && card.Right <= size.Width - FoodDialog.Inset + .5 &&
                    card.Bottom <= size.Height - FoodDialog.Inset + .5 && Math.Abs(card.X - (size.Width - card.Right)) < 1 &&
                    Math.Abs(card.Y - (size.Height - card.Bottom)) < 1 && Math.Abs(card.Width - FoodDialog.CardWidth) < .5,
                    $"{size.Width:0}x{size.Height:0}: the dialog is centered and not cut off ({card})");
                Ensure(Rect(sample.ScrimLayer, host).Size == size, $"{size.Width:0}x{size.Height:0}: the scrim covers the whole window");
            }
            finally { frame.Close(); }
        }
        Console.WriteLine("All Replate food dialog checks passed (open without adding, content, quantity and limit, adding, one restaurant, unavailable, Esc, X and scrim, focus trap and return, 1040x720 and 1920x1080).");
    }

    private static bool Inside(FoodDialog dialog, IInputElement? element) => element is Visual visual && dialog.Card.IsVisualAncestorOf(visual);

    private static Rect Rect(Visual visual, Visual root) =>
        new(visual.TranslatePoint(default, root) ?? throw new InvalidOperationException("Element is outside the window"), visual.Bounds.Size);

    private static Color ColorOf(IBrush? brush) => brush is ISolidColorBrush solid ? solid.Color : Colors.Transparent;

    private static async Task Pseudo(StyledElement element, string pseudo, bool on)
    { ((IPseudoClasses)element.Classes).Set(pseudo, on); await Task.Delay(100); }

    private static void Key(Control target, Key key, KeyModifiers modifiers = KeyModifiers.None) =>
        target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key, KeyModifiers = modifiers, Source = target });

    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static async Task Settle()
    { await Task.Delay(250); await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render); }

    private static void Ensure(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
