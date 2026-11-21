using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Replate.Domain.Entities;

namespace Replate.Desktop;

internal sealed class ListingFeed
{
    internal const string OneRestaurantMessage = "Keranjang hanya berisi menu dari satu restoran. Kosongkan keranjang untuk memesan dari restoran lain.";
    internal const string StockMessage = "Jumlah di keranjang sudah mencapai stok yang tersedia.";
    private readonly MainWindow owner;
    private readonly DemoSession session;
    private readonly Func<IEnumerable<SurplusListing>> source;
    private readonly Func<Control> empty;
    private readonly Dictionary<Guid, (Button Details, Button Add)> buttons = [];
    internal TextBlock Notice { get; } = Ui.Text("", 12, Ui.Green);
    internal StackPanel Panel { get; } = Ui.Stack(Ui.Spacing.Related);
    internal IReadOnlyList<SurplusListing> Listings { get; private set; } = [];
    internal event Action? Refreshed;

    internal ListingFeed(MainWindow owner, DemoSession session, Func<IEnumerable<SurplusListing>> source, Func<Control> empty)
    {
        this.owner = owner; this.session = session; this.source = source; this.empty = empty;
        Notice.IsVisible = false;
        Notice.HorizontalAlignment = HorizontalAlignment.Right; Notice.VerticalAlignment = VerticalAlignment.Center; Notice.TextWrapping = TextWrapping.NoWrap;
        Notice.Margin = new Thickness(Ui.Spacing.Related, 0, 0, 0);
    }

    // Listings
    internal void Refresh()
    {
        Listings = source().ToList();
        owner.UpdateCartCount();
        if (session.Cart.Items.Count == 0) Notice.IsVisible = false;
        Panel.Children.Clear();
        buttons.Clear();
        foreach (var listing in Listings) Panel.Children.Add(Card(listing));
        if (Listings.Count == 0) Panel.Children.Add(empty());
        Refreshed?.Invoke();
    }

    internal static Control EmptyState(string title, string message, Action? back)
    {
        var state = Ui.Stack(12, Ui.Text(title, 20, bold: true), Ui.Text(message, 13, Ui.Muted));
        if (back is not null)
        {
            var button = Ui.Button(CollectionView.BackLabel, back);
            button.HorizontalAlignment = HorizontalAlignment.Left;
            state.Children.Add(button);
        }
        return state;
    }

    private Control Card(SurplusListing listing)
    {
        var price = Ui.Stack(5, Ui.Text(DemoSession.Rupiah(listing.RescuePrice), 18, bold: true),
            new TextBlock { Text = DemoSession.Rupiah(listing.Menu.NormalPrice), FontSize = 12,
                Foreground = Ui.Muted, TextDecorations = TextDecorations.Strikethrough },
            Ui.Text($"Hemat {listing.CalculateDiscount():0}%", 12, Ui.Green));
        var add = Ui.Button("Tambah", () => Open(listing, true), true);
        AutomationProperties.SetName(add, $"Tambah {listing.Menu.Name} ke keranjang");
        var details = Ui.Button(listing.Menu.Name, () => Open(listing, false));
        details.Background = Brushes.Transparent; details.Padding = new Thickness(0); details.FontSize = 18;
        var description = Ui.Stack(6, details,
            Ui.Text(listing.Menu.Description, 13, Ui.Muted),
            Ui.Text($"{listing.Menu.Restaurant?.Name} · {listing.Menu.Category}", 12, Ui.Muted));
        description.Margin = new Thickness(0, 0, 24, 0);
        var pickup = Ui.Stack(5, Ui.Text("Pengambilan", 12, Ui.Muted),
            Ui.Text($"{listing.PickupStart.ToLocalTime():HH:mm}–{listing.PickupEnd.ToLocalTime():HH:mm}", 14),
            Ui.Text($"{listing.Quantity} porsi tersisa", 12, Ui.Muted));
        pickup.VerticalAlignment = price.VerticalAlignment = add.VerticalAlignment = description.VerticalAlignment = VerticalAlignment.Center;
        var photo = new MenuPhotoView(session.PhotoPath(listing.Menu.ImageUrl), 96, 96) { Margin = new Thickness(0, 0, 16, 0) };
        var card = new Border
        {
            Child = Ui.Columns("112,*,136,120,90", photo, description, pickup, price, add),
            Background = Brushes.White, CornerRadius = new CornerRadius(16), BoxShadow = MarketplaceView.CardShadow, Padding = new Thickness(20),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        card.Tapped += (_, e) =>
        {
            if ((e.Source as Visual)?.FindAncestorOfType<Button>(true) is { } button && card.IsVisualAncestorOf(button)) return;
            Open(listing, false);
        };
        buttons[listing.Id] = (details, add);
        return card;
    }

    // Dialog
    internal Button AddButton(SurplusListing listing) => buttons[listing.Id].Add;
    internal Button DetailsButton(SurplusListing listing) => buttons[listing.Id].Details;

    internal void Open(SurplusListing listing, bool fromAdd)
    {
        var trigger = buttons.TryGetValue(listing.Id, out var pair) ? fromAdd ? pair.Add : pair.Details : null;
        var method = trigger?.Classes.Contains(":focus-visible") == true ? NavigationMethod.Tab : NavigationMethod.Pointer;
        owner.ShowFoodDialog(listing, Add, method == NavigationMethod.Tab, () =>
        {
            if (buttons.TryGetValue(listing.Id, out var current)) (fromAdd ? current.Add : current.Details).Focus(method);
        });
    }

    // Cart
    internal string? Add(SurplusListing listing, int quantity = 1)
    {
        var otherRestaurant = session.Cart.Items.Count > 0 && session.Cart.RestaurantId != listing.Menu.RestaurantId;
        try { session.AddToCart(listing, quantity); }
        catch (InvalidOperationException) { return otherRestaurant ? OneRestaurantMessage : StockMessage; }
        Notice.Text = $"{listing.Menu.Name} ditambahkan ke keranjang.";
        Notice.Foreground = Ui.Green;
        Notice.IsVisible = true;
        Refresh();
        return null;
    }
}
