using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Replate.Domain.Entities;
using Path = Avalonia.Controls.Shapes.Path;

namespace Replate.Desktop;

public sealed class CartView : UserControl
{
    internal const string TitleText = "Keranjang";
    internal const double SummaryWidth = 340;
    internal const double PhotoSize = 72;
    internal const double CardPadding = 20;
    internal const double SummaryPadding = 24;
    internal const double ShadowInset = 24;
    internal const double RemoveSize = 36;
    internal const string RemoveClass = "cart-remove";
    internal const string SummaryTitle = "Ringkasan pesanan";
    internal const string CheckoutLabel = "Pesan sekarang";
    internal const string PickupNote = "Ambil langsung di restoran. Reservasi tidak melibatkan pembayaran.";
    internal const string UnavailableText = "Penawaran tidak tersedia. Hapus untuk melanjutkan.";
    internal const string UnavailableReason = "Hapus penawaran yang tidak tersedia untuk melanjutkan.";
    internal const string InvalidReason = "Periksa jumlah pesanan untuk melanjutkan.";
    internal const string EmptyTitle = "Keranjangmu masih kosong";
    internal const string EmptyMessage = "Mulai jelajahi menu dan selamatkan makanan hari ini.";
    internal const string EmptyAction = "Jelajahi menu";
    internal static readonly Geometry TrashIcon = Geometry.Parse(
        "M4 7H20 M9.5 7V4.5H14.5V7 M6.5 7L7.4 19.6A1.5 1.5 0 0 0 8.9 21H15.1A1.5 1.5 0 0 0 16.6 19.6L17.5 7 M10 11V17 M14 11V17");

    private readonly MainWindow host;
    private readonly DemoSession session;
    private readonly List<Line> lines = [];
    internal Button BackLink { get; }
    internal Path BackGlyph { get; }
    internal TextBlock Title { get; }
    internal TextBlock Subtitle { get; }
    internal StackPanel Header { get; }
    internal Grid Body { get; }
    internal Grid? Columns { get; }
    internal StackPanel? ItemsPanel { get; }
    internal ScrollViewer? ListScroll { get; }
    internal Border? Summary { get; }
    internal TextBlock? CountText { get; }
    internal TextBlock? RestaurantText { get; }
    internal TextBlock? TotalText { get; }
    internal TextBlock? CheckoutReason { get; }
    internal Button? CheckoutButton { get; }
    internal StackPanel? EmptyState { get; }
    internal Path? EmptyIcon { get; }
    internal Button? EmptyButton { get; }
    internal IReadOnlyList<Line> Lines => lines;

    internal CartView(MainWindow host, DemoSession session)
    {
        this.host = host; this.session = session;
        CollectionView.AddBackLinkStyles(Styles);
        AddStyles();

        // Back link
        BackLink = CollectionView.CreateBackLink(host.ShowMarketplace, out var glyph);
        BackGlyph = glyph;

        // Header
        Title = Ui.Text(TitleText, MarketplaceView.TitleSize, Ui.Ink, true);
        Title.FontWeight = FontWeight.Bold; Title.TextWrapping = TextWrapping.NoWrap; Title.HorizontalAlignment = HorizontalAlignment.Left;
        var restaurant = session.FindRestaurant(session.Cart.RestaurantId);
        Subtitle = Ui.Text(restaurant is null ? "" : $"Pesanan dari {restaurant.Name}", 14, Ui.Muted);
        Subtitle.IsVisible = session.Cart.Items.Count > 0 && restaurant is not null;
        Header = Ui.Stack(CollectionView.IntroGap, Title, Subtitle);

        // Layout
        Body = new Grid
        {
            RowDefinitions = RowDefinitions.Parse("Auto,Auto,*"), RowSpacing = Ui.Spacing.Related,
            Margin = new Thickness(Ui.Spacing.Page, Ui.Spacing.Section, Ui.Spacing.Page, 0),
            MaxWidth = MarketplaceView.ContentMaxWidth, HorizontalAlignment = HorizontalAlignment.Stretch
        };
        Body.Children.Add(BackLink);
        Grid.SetRow(Header, 1); Body.Children.Add(Header);
        Content = Body;

        // Empty
        if (session.Cart.Items.Count == 0)
        {
            EmptyIcon = new Path
            {
                Data = CustomerTopBar.CartIcon, Width = 72, Height = 72, Stretch = Stretch.Uniform, StrokeThickness = 2,
                StrokeLineCap = PenLineCap.Round, StrokeJoin = PenLineJoin.Round, Stroke = Ui.Green, HorizontalAlignment = HorizontalAlignment.Center
            };
            var heading = Ui.Text(EmptyTitle, 22, Ui.Ink, true);
            var message = Ui.Text(EmptyMessage, 15, Ui.Muted);
            heading.TextAlignment = message.TextAlignment = TextAlignment.Center;
            EmptyButton = Ui.Button(EmptyAction, host.ShowMarketplace, true);
            EmptyButton.HorizontalAlignment = HorizontalAlignment.Center;
            EmptyState = Ui.Stack(Ui.Spacing.Related, EmptyIcon, Ui.Stack(8, heading, message), EmptyButton);
            EmptyState.MaxWidth = 480; EmptyState.Margin = new Thickness(0, 0, 0, Ui.Spacing.Page);
            EmptyState.HorizontalAlignment = HorizontalAlignment.Center; EmptyState.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetRow(EmptyState, 2); Body.Children.Add(EmptyState);
            return;
        }

        // Items
        ItemsPanel = Ui.Stack(Ui.Spacing.Related);
        ItemsPanel.Margin = new Thickness(ShadowInset, 8, ShadowInset, Ui.Spacing.Page);
        foreach (var item in session.Cart.Items)
        {
            var line = new Line(this, item);
            lines.Add(line);
            ItemsPanel.Children.Add(line.Card);
        }
        ListScroll = Ui.Scroll(ItemsPanel);
        ListScroll.Margin = new Thickness(-ShadowInset, -8, -ShadowInset, 0);

        // Summary
        CountText = Value("");
        RestaurantText = Value(restaurant?.Name ?? "");
        RestaurantText.TextAlignment = TextAlignment.Right;
        TotalText = Ui.Text("", 24, Ui.Green, true);
        TotalText.FontWeight = FontWeight.Bold; TotalText.HorizontalAlignment = HorizontalAlignment.Right;
        CheckoutButton = Ui.FullWidth(Ui.Button(CheckoutLabel, async () =>
        {
            await host.RunAsync(async () =>
            {
                await session.CheckoutAsync();
                host.ShowOrder(session.LastOrder!, false);
            }, "Pesanan berhasil dibuat.", "Pesanan belum dapat dibuat. Periksa koneksi PostgreSQL lalu coba lagi.");
        }, true));
        CheckoutButton.HorizontalContentAlignment = HorizontalAlignment.Center;
        CheckoutReason = Ui.Text("", 12, Ui.Muted);
        var total = Ui.Columns("Auto,*", Ui.Text("Total", 16, Ui.Ink, true), TotalText);
        var summary = Ui.Stack(Ui.Spacing.Related,
            Ui.Text(SummaryTitle, 20, Ui.Ink, true),
            Ui.Stack(12, Row("Jumlah item", CountText), Row("Restoran", RestaurantText)),
            Ui.Divider(), total,
            Ui.Text(PickupNote, 12, Ui.Muted),
            Ui.Stack(8, CheckoutButton, CheckoutReason));
        Summary = new Border
        {
            Child = summary, Width = SummaryWidth, Padding = new Thickness(SummaryPadding), VerticalAlignment = VerticalAlignment.Top,
            Background = Brushes.White, CornerRadius = new CornerRadius(16), BoxShadow = MarketplaceView.CardShadow
        };
        AutomationProperties.SetName(Summary, SummaryTitle);

        Columns = new Grid { ColumnDefinitions = ColumnDefinitions.Parse($"*,{SummaryWidth}"), ColumnSpacing = Ui.Spacing.Section };
        Columns.Children.Add(ListScroll);
        Grid.SetColumn(Summary, 1); Columns.Children.Add(Summary);
        Grid.SetRow(Columns, 2); Body.Children.Add(Columns);
        Refresh();
    }

    // State
    internal void Refresh()
    {
        var now = DateTime.UtcNow;
        foreach (var line in lines) line.Update(now);
        var cart = session.Cart;
        CountText!.Text = $"{cart.Items.Sum(i => i.Quantity)} porsi";
        TotalText!.Text = DemoSession.Rupiah(cart.CalculateSubtotal());
        var unavailable = cart.Items.Any(i => !Available(i, now));
        CheckoutButton!.IsEnabled = cart.ValidateCart(now) && cart.Items.All(i => i.Listing.Menu.IsActive);
        CheckoutReason!.Text = CheckoutButton.IsEnabled ? "" : unavailable ? UnavailableReason : InvalidReason;
        CheckoutReason.IsVisible = !CheckoutButton.IsEnabled;
    }

    internal static bool Available(CartItem item, DateTime now) => item.Listing.IsAvailable(now) && item.Listing.Menu.IsActive;

    private void Change(CartItem item, int quantity) =>
        host.Run(() => session.ChangeQuantity(item.ListingId, quantity), quantity == 0 ? host.ShowCart : Refresh);

    // Controls
    private static TextBlock Value(string text)
    {
        var value = Ui.Text(text, 14, Ui.Ink, true);
        value.HorizontalAlignment = HorizontalAlignment.Right;
        return value;
    }

    private static Grid Row(string label, TextBlock value)
    {
        var row = Ui.Columns("Auto,*", Ui.Text(label, 14, Ui.Muted), value);
        value.Margin = new Thickness(Ui.Spacing.Related, 0, 0, 0);
        return row;
    }

    private void AddStyles()
    {
        Styles.Add(new Style(x => x.OfType<Button>().Class(RemoveClass).Not(y => y.Class(":pointerover")).Not(y => y.Class(":pressed"))) { Setters = { new Setter(TemplatedControl.ForegroundProperty, Ui.Muted) } });
    }

    // Item card
    internal sealed class Line
    {
        internal CartItem Item { get; }
        internal Border Card { get; }
        internal MenuPhotoView Photo { get; }
        internal TextBlock NameText { get; }
        internal TextBlock MetaText { get; }
        internal TextBlock PickupText { get; }
        internal TextBlock PriceText { get; }
        internal QuantityStepper Stepper { get; }
        internal TextBlock SubtotalText { get; }
        internal Button RemoveButton { get; }
        internal Path RemoveGlyph { get; }

        internal Line(CartView view, CartItem item)
        {
            Item = item;
            var listing = item.Listing; var menu = listing.Menu;
            Photo = new MenuPhotoView(view.session.PhotoPath(menu.ImageUrl), PhotoSize, PhotoSize, placeholderSize: 11)
            { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, Ui.Spacing.Related, 0) };
            NameText = Ui.Text(menu.Name, 18, Ui.Ink, true);
            MetaText = Ui.Text($"{menu.Restaurant?.Name} · {menu.Category}", 13, Ui.Muted);
            PickupText = Ui.Text($"Pengambilan {listing.PickupStart.ToLocalTime():HH:mm}–{listing.PickupEnd.ToLocalTime():HH:mm}", 13, Ui.Muted);
            PriceText = Ui.Text("", 14);
            var information = Ui.Stack(4, NameText, MetaText, PickupText, PriceText);
            information.VerticalAlignment = VerticalAlignment.Center;

            Stepper = new QuantityStepper($"Kurangi jumlah {menu.Name}", $"Tambah jumlah {menu.Name}", $"Jumlah {menu.Name}",
                delta => view.Change(item, item.Quantity + delta));
            Stepper.Margin = new Thickness(Ui.Spacing.Related, 0, 0, 0);
            SubtotalText = Ui.Text("", 18, Ui.Ink, true);
            SubtotalText.FontWeight = FontWeight.Bold; SubtotalText.TextAlignment = TextAlignment.Right;
            SubtotalText.MinWidth = 104; SubtotalText.TextWrapping = TextWrapping.NoWrap;
            SubtotalText.Margin = new Thickness(Ui.Spacing.Related, 0, 0, 0);

            RemoveGlyph = new Path
            {
                Data = TrashIcon, Width = 24, Height = 24, Stretch = Stretch.None, StrokeThickness = 1.75,
                StrokeLineCap = PenLineCap.Round, StrokeJoin = PenLineJoin.Round
            };
            RemoveButton = Ui.Round(new Button
            {
                Content = RemoveGlyph, BorderThickness = default, Margin = new Thickness(8, 0, -6, 0), VerticalAlignment = VerticalAlignment.Center,
                Cursor = new Cursor(StandardCursorType.Hand)
            }, RemoveSize, true);
            RemoveButton.Classes.Add(RemoveClass);
            RemoveGlyph[!Shape.StrokeProperty] = RemoveButton[!TemplatedControl.ForegroundProperty];
            AutomationProperties.SetName(RemoveButton, $"Hapus {menu.Name}");
            ToolTip.SetTip(RemoveButton, $"Hapus {menu.Name}");
            RemoveButton.Click += (_, _) => view.Change(item, 0);

            Card = new Border
            {
                Child = Ui.Columns("Auto,*,Auto,Auto,Auto", Photo, information, Stepper, SubtotalText, RemoveButton),
                Background = Brushes.White, CornerRadius = new CornerRadius(16), BoxShadow = MarketplaceView.CardShadow,
                Padding = new Thickness(CardPadding)
            };
            AutomationProperties.SetName(Card, menu.Name);
        }

        internal void Update(DateTime now)
        {
            var available = Available(Item, now);
            PriceText.Text = available ? $"{DemoSession.Rupiah(Item.UnitPrice)} / porsi" : UnavailableText;
            PriceText.Foreground = available ? Ui.Ink : Ui.Muted;
            Stepper.Show(Item.Quantity, available && Item.Quantity > 1, available && Item.Quantity < Item.Listing.Quantity);
            SubtotalText.Text = DemoSession.Rupiah(Item.CalculateSubtotal());
        }
    }
}
