using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using Replate.Domain.Entities;
using Replate.Domain.Enums;
using Path = Avalonia.Controls.Shapes.Path;

namespace Replate.Desktop;

public sealed partial class OrdersView
{
    internal const string TitleText = "Pesanan saya";
    internal const string All = "Semua";
    internal const string Active = "Aktif";
    internal const string Done = "Selesai";
    internal const string Cancelled = "Dibatalkan";
    internal static readonly string[] Filters = [All, Active, Done, Cancelled];
    internal const string FilterClass = "order-filter";
    internal const string SelectedClass = Ui.PillSelectedClass;
    internal const double FilterHeight = Ui.PillHeight;
    internal const double FilterGap = Ui.PillGap;
    internal const string EmptyTitle = "Belum ada pesanan";
    internal const string EmptyMessage = "Pesananmu akan muncul di sini setelah kamu memesan.";
    internal const string EmptyAction = "Jelajahi menu";
    internal const string FilterEmptyTitle = "Tidak ada pesanan di sini";
    internal const string FilterEmptyAction = "Lihat semua pesanan";
    internal const string DetailLabel = "Lihat detail";
    internal const string CodeLabel = "Kode ambil";
    internal const int SummaryRows = 3;
    internal const double CardPadding = 20;
    internal const double TotalSize = 22;
    internal const double CodeSize = 18;
    internal static string MoreText(int count) => $"+{count} menu lainnya";

    internal static bool InFilter(Order order, string filter) => filter switch
    {
        Active => IsActive(order),
        Done => order.Status == OrderStatus.Completed,
        Cancelled => order.Status == OrderStatus.Cancelled,
        _ => true
    };
    internal static bool IsActive(Order order) => order.Status is not (OrderStatus.Completed or OrderStatus.Cancelled);
    internal static string OrderedText(Order order) => order.OrderedAt.ToLocalTime().ToString("dd MMM yyyy, HH:mm");
    internal static string? PickupWindow(Order order)
    {
        if (order.Items.Count == 0) return null;
        var start = order.Items.Min(i => i.Listing.PickupStart).ToLocalTime();
        var end = order.Items.Max(i => i.Listing.PickupEnd).ToLocalTime();
        return start.Date == end.Date ? $"{start:dd MMM, HH:mm}–{end:HH:mm}" : $"{start:dd MMM, HH:mm}–{end:dd MMM, HH:mm}";
    }
    internal static string MetaText(Order order) =>
        PickupWindow(order) is { } window ? $"Dipesan {OrderedText(order)} · Pengambilan {window}" : $"Dipesan {OrderedText(order)}";

    private MainWindow host = null!;
    private DemoSession session = null!;
    private readonly List<Button> filterButtons = [];
    private readonly List<OrderCard> cards = [];
    internal string SelectedFilter { get; private set; } = All;
    internal Button? BackLink { get; private set; }
    internal TextBlock? Title { get; private set; }
    internal StackPanel? FilterRow { get; private set; }
    internal StackPanel? Header { get; private set; }
    internal Grid? Body { get; private set; }
    internal ContentControl? Results { get; private set; }
    internal StackPanel? ItemsPanel { get; private set; }
    internal ScrollViewer? ListScroll { get; private set; }
    internal StackPanel? EmptyState { get; private set; }
    internal Path? EmptyIcon { get; private set; }
    internal TextBlock? EmptyHeading { get; private set; }
    internal Button? EmptyButton { get; private set; }
    internal IReadOnlyList<Button> FilterButtons => filterButtons;
    internal IReadOnlyList<OrderCard> Cards => cards;

    private void BuildCustomer(MainWindow owner, DemoSession source)
    {
        host = owner; session = source;
        CollectionView.AddTextLinkStyles(Styles);
        AddFilterStyles(Styles);

        // Header
        BackLink = CollectionView.CreateBackLink(host.ShowMarketplace, out _);
        Title = Ui.Text(TitleText, MarketplaceView.TitleSize, Ui.Ink, true);
        Title.FontWeight = FontWeight.Bold; Title.TextWrapping = TextWrapping.NoWrap; Title.HorizontalAlignment = HorizontalAlignment.Left;
        FilterRow = Ui.Row(FilterGap, [.. Filters.Select(FilterButton)]);
        Header = Ui.Stack(Ui.Spacing.Related, BackLink, Title, FilterRow);

        // Layout
        Results = new ContentControl { HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch };
        Body = new Grid
        {
            RowDefinitions = RowDefinitions.Parse("Auto,*"), RowSpacing = Ui.Spacing.Related,
            Margin = new Thickness(Ui.Spacing.Page, Ui.Spacing.Section, Ui.Spacing.Page, 0),
            MaxWidth = MarketplaceView.ContentMaxWidth, HorizontalAlignment = HorizontalAlignment.Stretch
        };
        Body.Children.Add(Header);
        Grid.SetRow(Results, 1); Body.Children.Add(Results);
        Content = Body;
        Select(All);
    }

    // Filter
    private Button FilterButton(string label)
    {
        var button = Ui.Pill(label, FilterClass, 0, () => Select(label));
        filterButtons.Add(button);
        return button;
    }

    internal static void AddFilterStyles(Styles styles) => styles.Add(Ui.PillStyles(FilterClass, Ui.QuietPills));

    // State
    internal void Select(string filter)
    {
        SelectedFilter = filter;
        foreach (var button in filterButtons) button.Classes.Set(SelectedClass, button.Content as string == filter);
        cards.Clear();
        ItemsPanel = null; ListScroll = null; EmptyState = null; EmptyIcon = null; EmptyHeading = null; EmptyButton = null;
        var orders = session.CustomerOrders.ToList();
        FilterRow!.IsVisible = orders.Count > 0;
        var visible = orders.Where(o => InFilter(o, filter)).ToList();
        if (visible.Count == 0) { Results!.Content = Empty(orders.Count == 0); return; }
        ItemsPanel = Ui.Stack(Ui.Spacing.Related);
        foreach (var order in visible)
        {
            var card = new OrderCard(host, session, order);
            cards.Add(card);
            ItemsPanel.Children.Add(card.Card);
        }
        ItemsPanel.Margin = new Thickness(CartView.ShadowInset, 8, CartView.ShadowInset, Ui.Spacing.Page);
        ListScroll = Ui.Scroll(ItemsPanel);
        ListScroll.Margin = new Thickness(-CartView.ShadowInset, -8, -CartView.ShadowInset, 0);
        Results!.Content = ListScroll;
    }

    // Empty
    private StackPanel Empty(bool noHistory)
    {
        EmptyIcon = Ui.EmptyIcon(Ui.ReceiptOutline);
        EmptyHeading = Ui.Text(noHistory ? EmptyTitle : FilterEmptyTitle, 22, Ui.Ink, true);
        EmptyHeading.TextAlignment = TextAlignment.Center;
        var texts = Ui.Stack(8, EmptyHeading);
        if (noHistory)
        {
            var message = Ui.Text(EmptyMessage, 15, Ui.Muted);
            message.TextAlignment = TextAlignment.Center;
            texts.Children.Add(message);
        }
        EmptyButton = noHistory ? Ui.Button(EmptyAction, host.ShowMarketplace, true) : Ui.Button(FilterEmptyAction, () => { Select(All); filterButtons[0].Focus(); }, true);
        EmptyButton.HorizontalAlignment = HorizontalAlignment.Center;
        if (!noHistory) EmptyButton.Margin = new Thickness(0, Ui.Spacing.Section - Ui.Spacing.Related, 0, 0);
        EmptyState = Ui.Stack(Ui.Spacing.Related, EmptyIcon, texts, EmptyButton);
        EmptyState.MaxWidth = 480; EmptyState.Margin = new Thickness(0, 0, 0, Ui.Spacing.Page);
        EmptyState.HorizontalAlignment = HorizontalAlignment.Center; EmptyState.VerticalAlignment = VerticalAlignment.Center;
        return EmptyState;
    }

    // Order card
    internal sealed class OrderCard
    {
        internal Order Order { get; }
        internal Border Card { get; }
        internal TextBlock NameText { get; }
        internal OrderStatusBadge Badge { get; }
        internal TextBlock Meta { get; }
        internal IReadOnlyList<Grid> ItemRows { get; }
        internal TextBlock? MoreLine { get; }
        internal StackPanel? CodeBlock { get; }
        internal TextBlock? CodeText { get; }
        internal Border DividerLine { get; }
        internal Grid FooterRow { get; }
        internal TextBlock TotalLabel { get; }
        internal TextBlock TotalText { get; }
        internal Button DetailButton { get; }

        internal OrderCard(MainWindow host, DemoSession session, Order order)
        {
            Order = order;

            // Heading
            NameText = Ui.Text(session.FindRestaurant(order.RestaurantId)?.Name ?? CartView.MissingRestaurant, 18, Ui.Ink, true);
            NameText.FontWeight = FontWeight.Bold;
            Badge = new OrderStatusBadge(order) { HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(Ui.Spacing.Related, 0, 0, 0) };
            Meta = Ui.Text(MetaText(order), 13, Ui.Muted);
            var heading = Ui.Stack(4, Ui.Columns("*,Auto", NameText, Badge), Meta);

            // Items
            ItemRows = order.Items.Take(SummaryRows).Select(item =>
            {
                var quantity = Ui.Text($"{item.Quantity} x", 14, Ui.Ink);
                quantity.TextAlignment = TextAlignment.Right; quantity.TextWrapping = TextWrapping.NoWrap;
                quantity.Margin = new Thickness(Ui.Spacing.Related, 0, 0, 0);
                return Ui.Columns("*,Auto", Ui.Text(item.MenuName, 14, Ui.Ink), quantity);
            }).ToList();
            var items = Ui.Stack(8, [.. ItemRows]);
            if (order.Items.Count > SummaryRows)
            {
                MoreLine = Ui.Text(MoreText(order.Items.Count - SummaryRows), 13, Ui.Muted);
                items.Children.Add(MoreLine);
            }
            var body = Ui.Stack(Ui.Spacing.Related, heading, items);

            // Pickup code
            if (IsActive(order) && !string.IsNullOrWhiteSpace(order.PickupCode))
            {
                CodeText = Ui.Text(order.PickupCode, CodeSize, Ui.Ink, true);
                CodeText.FontFamily = Ui.CodeFont; CodeText.FontWeight = FontWeight.Bold; CodeText.TextWrapping = TextWrapping.NoWrap;
                CodeBlock = Ui.Stack(4, Ui.Text(CodeLabel, 12, Ui.Muted), CodeText);
                AutomationProperties.SetName(CodeBlock, $"{CodeLabel} {order.PickupCode}");
                body.Children.Add(CodeBlock);
            }

            // Footer
            DividerLine = Ui.Divider();
            TotalLabel = Ui.Text(CartView.TotalLabelText, 12, Ui.Muted);
            TotalText = Ui.Text(DemoSession.Rupiah(order.TotalPrice), TotalSize, Ui.Green, true);
            TotalText.FontWeight = FontWeight.Bold; TotalText.TextWrapping = TextWrapping.NoWrap;
            var total = Ui.Stack(4, TotalLabel, TotalText);
            total.VerticalAlignment = VerticalAlignment.Center; total.HorizontalAlignment = HorizontalAlignment.Left;
            DetailButton = CollectionView.CreateTextLink(DetailLabel, () => host.ShowOrder(order, false));
            DetailButton.VerticalAlignment = VerticalAlignment.Center; DetailButton.HorizontalAlignment = HorizontalAlignment.Right;
            DetailButton.Margin = new Thickness(0, 0, -8, 0);
            AutomationProperties.SetName(DetailButton, $"Lihat detail pesanan dari {NameText.Text}");
            FooterRow = Ui.Columns("*,Auto", total, DetailButton);
            body.Children.Add(DividerLine);
            body.Children.Add(FooterRow);

            Card = new Border
            {
                Child = body, Background = Brushes.White, CornerRadius = new CornerRadius(16), BoxShadow = MarketplaceView.CardShadow,
                Padding = new Thickness(CardPadding)
            };
            AutomationProperties.SetName(Card, $"Pesanan dari {NameText.Text}");
        }
    }
}

public sealed partial class OrderDetailView
{
    internal const string TitleText = "Detail pesanan";
    internal const string BackText = "Kembali ke Pesanan saya";
    internal const string CodeCaption = "Tunjukkan kode ini di restoran.";
    internal const string ItemsTitle = "Item pesanan";
    internal const double CodeSize = 36;
    internal const double CardPadding = 24;
    internal const string ColumnLayout = "3*,2*";
    internal Button? BackLink { get; private set; }
    internal TextBlock? Title { get; private set; }
    internal OrderStatusBadge? Badge { get; private set; }
    internal StackPanel? Body { get; private set; }
    internal ScrollViewer? Scroll { get; private set; }
    internal Grid? ColumnsGrid { get; private set; }
    internal StackPanel? Side { get; private set; }
    internal Border? CodeCard { get; private set; }
    internal TextBlock? CodeCaptionText { get; private set; }
    internal Border? InfoCard { get; private set; }
    internal TextBlock? RestaurantText { get; private set; }
    internal Border? ItemsCard { get; private set; }
    internal IReadOnlyList<ItemRow> ItemRows { get; private set; } = [];
    internal TextBlock? TotalText { get; private set; }
    internal Button? CancelOrderButton { get; private set; }

    internal sealed record ItemRow(OrderItem Item, Grid Row, TextBlock Name, TextBlock Quantity, TextBlock Subtotal);

    private static Border DetailCard(Control child) => new()
    {
        Child = child, Background = Brushes.White, CornerRadius = new CornerRadius(16), BoxShadow = MarketplaceView.CardShadow,
        Padding = new Thickness(CardPadding), VerticalAlignment = VerticalAlignment.Top
    };

    internal string? CopyRequest { get; private set; }
    private async Task CopyPickupCode(string pickupCode)
    {
        CopyRequest = pickupCode;
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard ?? throw new InvalidOperationException("Clipboard unavailable");
        for (var attempt = 1; ; attempt++)
        {
            try { await clipboard.SetTextAsync(pickupCode); return; }
            catch (Exception) when (attempt < 3) { await Task.Delay(50); }
        }
    }

    private static StackPanel Fact(string label, string value) => Ui.Stack(4, Ui.Text(label, 12, Ui.Muted), Ui.Text(value, 14, Ui.Ink));

    private void BuildCustomer(MainWindow host, DemoSession session, Order order)
    {
        CollectionView.AddTextLinkStyles(Styles);

        // Header
        BackLink = CollectionView.CreateBackLink(host.ShowCustomerOrders, out _, BackText);
        Title = Ui.Text(TitleText, MarketplaceView.TitleSize, Ui.Ink, true);
        Title.FontWeight = FontWeight.Bold; Title.TextWrapping = TextWrapping.NoWrap;
        Badge = new OrderStatusBadge(order);
        var header = Ui.Stack(CollectionView.IntroGap, Ui.Row(Ui.Spacing.Related, Title, Badge), Ui.Text(StatusNote(order), 14, Ui.Muted));

        // Pickup code
        var pickupCode = order.PickupCode;
        PickupCodeText = Ui.Text(pickupCode, CodeSize, Ui.Green, true);
        PickupCodeText.FontFamily = Ui.CodeFont; PickupCodeText.FontWeight = FontWeight.Bold; PickupCodeText.TextWrapping = TextWrapping.NoWrap;
        var code = Ui.Stack(12, Ui.Text(OrdersView.CodeLabel, 14, Ui.Muted, true));
        if (order.Status != OrderStatus.Cancelled && !string.IsNullOrWhiteSpace(pickupCode))
        {
            CopyCodeButton = Ui.Button("", async () =>
            {
                CopyCodeButton!.IsEnabled = false;
                try
                {
                    await host.RunAsync(() => CopyPickupCode(pickupCode), "Kode berhasil disalin.", "Kode belum berhasil disalin. Silakan coba lagi.");
                }
                finally { CopyCodeButton!.IsEnabled = true; }
            });
            CopyCodeButton.Content = new PathIcon
            {
                Width = 18, Height = 18, Foreground = Ui.Green,
                Data = Geometry.Parse("M8 2H20V18H16V22H2V6H8V2ZM10 4V6H16V16H18V4H10ZM4 8V20H14V8H4Z")
            };
            Ui.Round(CopyCodeButton, 44, true);
            CopyCodeButton.BorderThickness = default;
            CopyCodeButton.Focusable = true;
            ToolTip.SetTip(CopyCodeButton, "Salin kode pengambilan");
            AutomationProperties.SetName(CopyCodeButton, "Salin kode pengambilan");
            code.Children.Add(Ui.Row(12, PickupCodeText, CopyCodeButton));
        }
        else code.Children.Add(PickupCodeText);
        if (order.Status != OrderStatus.Cancelled)
        {
            CodeCaptionText = Ui.Text(CodeCaption, 14, Ui.Muted);
            code.Children.Add(CodeCaptionText);
        }
        CodeCard = DetailCard(code);

        // Restaurant
        var restaurant = session.FindRestaurant(order.RestaurantId);
        RestaurantText = Ui.Text(restaurant?.Name ?? CartView.MissingRestaurant, 18, Ui.Ink, true);
        RestaurantText.FontWeight = FontWeight.Bold;
        var info = Ui.Stack(Ui.Spacing.Related, Ui.Stack(4, RestaurantText, Ui.Text(restaurant?.Address ?? "", 13, Ui.Muted)));
        if (OrdersView.PickupWindow(order) is { } window) info.Children.Add(Fact("Pengambilan", window));
        info.Children.Add(Fact("Dipesan", OrdersView.OrderedText(order)));
        info.Children.Add(Fact("Nomor pesanan", $"#{order.Id.ToString("N")[..8].ToUpperInvariant()}"));
        InfoCard = DetailCard(info);

        // Items
        ItemRows = order.Items.Select(item =>
        {
            var name = Ui.Text(item.MenuName, 14, Ui.Ink, true);
            var quantity = Ui.Text($"{item.Quantity} x {DemoSession.Rupiah(item.UnitPrice)}", 13, Ui.Muted);
            var subtotal = Ui.Text(DemoSession.Rupiah(item.CalculateSubtotal()), 14, Ui.Ink, true);
            subtotal.TextAlignment = TextAlignment.Right; subtotal.TextWrapping = TextWrapping.NoWrap;
            subtotal.Margin = new Thickness(Ui.Spacing.Related, 0, 0, 0);
            return new ItemRow(item, Ui.Columns("*,Auto", Ui.Stack(4, name, quantity), subtotal), name, quantity, subtotal);
        }).ToList();
        var itemsTitle = Ui.Text(ItemsTitle, 18, Ui.Ink, true);
        itemsTitle.FontWeight = FontWeight.Bold;
        TotalText = Ui.Text(DemoSession.Rupiah(order.TotalPrice), CartView.TotalSize, Ui.Green, true);
        TotalText.FontWeight = FontWeight.Bold; TotalText.TextWrapping = TextWrapping.NoWrap;
        var total = Ui.Stack(4, Ui.Text(CartView.TotalLabelText, 12, Ui.Muted), TotalText);
        ItemsCard = DetailCard(Ui.Stack(Ui.Spacing.Related, itemsTitle, Ui.Stack(12, [.. ItemRows.Select(r => (Control)r.Row)]), Ui.Divider(), total));

        // Layout
        Side = Ui.Stack(Ui.Spacing.Related, CodeCard, InfoCard);
        if (OrdersView.IsActive(order))
        {
            CancelOrderButton = CancelButton(host, session, order, false);
            CancelOrderButton.HorizontalAlignment = HorizontalAlignment.Left;
            Side.Children.Add(CancelOrderButton);
        }
        ColumnsGrid = Ui.Columns(ColumnLayout, ItemsCard, Side);
        ColumnsGrid.ColumnSpacing = Ui.Spacing.Section;
        Body = Ui.Stack(Ui.Spacing.Related, BackLink, header, ColumnsGrid);
        Body.Margin = new Thickness(Ui.Spacing.Page, Ui.Spacing.Section, Ui.Spacing.Page, Ui.Spacing.Page);
        Body.MaxWidth = MarketplaceView.ContentMaxWidth; Body.HorizontalAlignment = HorizontalAlignment.Stretch;
        Scroll = Ui.Scroll(Body);
        Content = Scroll;
    }
}
