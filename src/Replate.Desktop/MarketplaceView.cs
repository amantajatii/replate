using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Replate.Domain.Entities;
using Path = Avalonia.Controls.Shapes.Path;

namespace Replate.Desktop;

public sealed class MarketplaceView : UserControl
{
    internal const double ContentMaxWidth = 1200;
    internal const string Cheapest = "Termurah";
    internal const string EndingSoon = "Segera berakhir";
    internal const string SectionTitle = "Jelajahi Menu";
    internal static readonly string[] FilterNames = [Cheapest, EndingSoon, "Makanan utama", "Roti & kue", "Vegetarian"];
    internal static readonly BoxShadows CardShadow = BoxShadows.Parse("0 8 24 0 #1F23382C");
    private static readonly Dictionary<string, Geometry> FilterIcons = new()
    {
        [Cheapest] = Geometry.Parse("M3.5 12.2V4.5A1 1 0 0 1 4.5 3.5H12.2L20.5 11.8A1.5 1.5 0 0 1 20.5 13.9L13.9 20.5A1.5 1.5 0 0 1 11.8 20.5Z M8 6.5A1.5 1.5 0 1 1 8 9.5A1.5 1.5 0 1 1 8 6.5Z"),
        [EndingSoon] = Geometry.Parse("M12 3A9 9 0 1 1 12 21A9 9 0 1 1 12 3Z M12 7V12L15.5 14"),
        ["Makanan utama"] = Geometry.Parse("M6 3V8A3 3 0 0 0 12 8V3 M9 3V21 M17 3C15.3 3 14.5 5.2 14.5 7.5C14.5 9.6 15.6 11 17 11C18.4 11 19.5 9.6 19.5 7.5C19.5 5.2 18.7 3 17 3Z M17 11V21"),
        ["Roti & kue"] = Geometry.Parse("M5 11.5C3.6 11 3 9.9 3 8.8C3 6.2 6.9 4.5 12 4.5C17.1 4.5 21 6.2 21 8.8C21 9.9 20.4 11 19 11.5V18.5A1.5 1.5 0 0 1 17.5 20H6.5A1.5 1.5 0 0 1 5 18.5Z M9 9L10.5 11 M13.5 9L15 11"),
        ["Vegetarian"] = Geometry.Parse("M5 19C5 10 10.5 4.5 20 4C19.5 13.5 14 19 5 19Z M5 19L13 11")
    };
    private static readonly ControlTheme CardButton = new(typeof(Button))
    {
        Setters =
        {
            new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<Button>((button, _) => Ui.FocusFrame(new Border
            {
                [!Border.BackgroundProperty] = button[!TemplatedControl.BackgroundProperty],
                [!Border.BorderBrushProperty] = button[!TemplatedControl.BorderBrushProperty],
                [!Border.BorderThicknessProperty] = button[!TemplatedControl.BorderThicknessProperty],
                [!Border.PaddingProperty] = button[!TemplatedControl.PaddingProperty],
                CornerRadius = new CornerRadius(16), BoxShadow = CardShadow,
                Child = new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [!ContentPresenter.ContentProperty] = button[!ContentControl.ContentProperty],
                    [!ContentPresenter.ForegroundProperty] = button[!TemplatedControl.ForegroundProperty],
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
                }
            }, new CornerRadius(16))))
        }
    };

    private readonly MainWindow owner;
    private readonly DemoSession session;
    private readonly TextBlock notice = Ui.Text("", 12, Ui.Green);
    private readonly Dictionary<string, Button> filters = [];
    internal MarketplaceBanner Banner { get; }
    internal Grid FilterRow { get; }
    internal IReadOnlyDictionary<string, Button> FilterCards => filters;
    internal StackPanel ListingsPanel { get; } = Ui.Stack(Ui.Spacing.Related);
    internal TextBlock Title { get; } = Ui.Text(SectionTitle, 20, Ui.Ink, true);
    internal Grid TitleRow { get; }
    internal StackPanel Section { get; }
    internal StackPanel Body { get; }
    internal ScrollViewer Scroll { get; }
    internal string? ActiveFilter { get; private set; }
    internal IReadOnlyList<SurplusListing> VisibleListings { get; private set; } = [];
    internal int VisibleCount => VisibleListings.Count;

    internal MarketplaceView(MainWindow owner, DemoSession session)
    {
        this.owner = owner; this.session = session;
        Banner = new MarketplaceBanner(session.CurrentUser?.Name ?? "");
        FilterRow = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("*,*,*,*,*"), ColumnSpacing = Ui.Spacing.Related };
        for (var i = 0; i < FilterNames.Length; i++)
        {
            var card = FilterCard(FilterNames[i]);
            Grid.SetColumn(card, i); FilterRow.Children.Add(card);
        }
        AddStyles();
        notice.IsVisible = false;
        notice.HorizontalAlignment = HorizontalAlignment.Right; notice.TextWrapping = TextWrapping.NoWrap;
        Title.HorizontalAlignment = HorizontalAlignment.Left; Title.TextWrapping = TextWrapping.NoWrap;
        TitleRow = Ui.Columns("Auto,*", Title, notice);
        Section = Ui.Stack(Ui.Spacing.Related, TitleRow, ListingsPanel);
        Body = Ui.Stack(Ui.Spacing.Section, Banner, FilterRow, Section,
            Ui.Text("Harga dan stok contoh. Reservasi tidak melibatkan pembayaran.", 12, Ui.Muted));
        Body.Margin = new Thickness(Ui.Spacing.Page, Ui.Spacing.Section, Ui.Spacing.Page, Ui.Spacing.Page); Body.MaxWidth = ContentMaxWidth;
        Body.HorizontalAlignment = HorizontalAlignment.Stretch;
        Scroll = Ui.Scroll(Body);
        Content = Scroll;
    }

    // Filters
    private Button FilterCard(string name)
    {
        var icon = new Path
        {
            Data = FilterIcons[name], Width = 24, Height = 24, Stretch = Stretch.None, Stroke = Ui.Green, StrokeThickness = 1.75,
            StrokeLineCap = PenLineCap.Round, StrokeJoin = PenLineJoin.Round, HorizontalAlignment = HorizontalAlignment.Center
        };
        var label = new TextBlock
        {
            Text = name, FontSize = 14, TextTrimming = TextTrimming.CharacterEllipsis,
            HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center
        };
        var card = new Button
        {
            Theme = CardButton, Content = Ui.Stack(8, icon, label), Height = 96, Padding = new Thickness(12, 16),
            BorderThickness = new Thickness(2), HorizontalAlignment = HorizontalAlignment.Stretch, ClipToBounds = false,
            HorizontalContentAlignment = HorizontalAlignment.Center, Cursor = new Cursor(StandardCursorType.Hand)
        };
        card.Classes.Add("filter-card");
        AutomationProperties.SetName(card, name);
        card.Click += (_, _) => SelectFilter(name);
        filters.Add(name, card);
        return card;
    }

    private void AddStyles()
    {
        var selection = Ui.Selection;
        Style State(Func<Selector, Selector> state, IBrush? background, IBrush? foreground, IBrush? border = null, FontWeight? weight = null)
        {
            var style = new Style(x => state(x.OfType<Button>().Class("filter-card")));
            if (background is not null) style.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, background));
            if (foreground is not null) style.Setters.Add(new Setter(TemplatedControl.ForegroundProperty, foreground));
            if (border is not null) style.Setters.Add(new Setter(TemplatedControl.BorderBrushProperty, border));
            if (weight is not null) style.Setters.Add(new Setter(TemplatedControl.FontWeightProperty, weight.Value));
            return style;
        }
        Styles.Add(State(x => x, Brushes.White, Ui.Ink, Brushes.Transparent, FontWeight.Normal));
        Styles.Add(State(x => x.Class(":pointerover"), selection, Ui.Green));
        Styles.Add(State(x => x.Class("active"), selection, Ui.Green, null, FontWeight.SemiBold));
        Styles.Add(State(x => x.Class("active").Class(":pointerover"), Brush.Parse("#DDE3DA"), Ui.Green));
    }

    internal void SelectFilter(string name)
    {
        ActiveFilter = ActiveFilter == name ? null : name;
        Refresh();
    }

    internal void ClearFilter()
    {
        ActiveFilter = null; Refresh();
    }

    // Listings
    internal void Refresh()
    {
        var category = ActiveFilter is null or Cheapest or EndingSoon ? "Semua" : ActiveFilter;
        var listings = session.Search(owner.SearchText, category);
        if (ActiveFilter == Cheapest) listings = listings.OrderBy(l => l.RescuePrice);
        else if (ActiveFilter == EndingSoon) listings = listings.OrderBy(l => l.PickupEnd);
        VisibleListings = listings.ToList();
        owner.UpdateCartCount();
        if (session.Cart.Items.Count == 0) notice.IsVisible = false;
        foreach (var (name, card) in filters) card.Classes.Set("active", name == ActiveFilter);
        ListingsPanel.Children.Clear();
        foreach (var listing in VisibleListings) ListingsPanel.Children.Add(FoodCard(listing));
        if (VisibleCount == 0)
        {
            var clear = Ui.Button("Hapus filter", () => { ActiveFilter = null; owner.ClearSearch(); Refresh(); });
            clear.HorizontalAlignment = HorizontalAlignment.Left;
            ListingsPanel.Children.Add(Ui.Stack(12, Ui.Text("Tidak ada menu yang cocok", 20, bold: true),
                Ui.Text("Coba kata kunci lain atau batalkan filter.", 13, Ui.Muted), clear));
        }
    }

    private Control FoodCard(SurplusListing listing)
    {
        var price = Ui.Stack(5, Ui.Text(DemoSession.Rupiah(listing.RescuePrice), 18, bold: true),
            new TextBlock { Text = DemoSession.Rupiah(listing.Menu.NormalPrice), FontSize = 12,
                Foreground = Ui.Muted, TextDecorations = TextDecorations.Strikethrough },
            Ui.Text($"Hemat {listing.CalculateDiscount():0}%", 12, Ui.Green));
        var add = Ui.Button("Tambah", () => Add(listing), true);
        AutomationProperties.SetName(add, $"Tambah {listing.Menu.Name} ke keranjang");
        var details = Ui.Button(listing.Menu.Name, () => owner.ShowFood(listing));
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
        return new Border
        {
            Child = Ui.Columns("112,*,136,120,90", photo, description, pickup, price, add),
            Background = Brushes.White, CornerRadius = new CornerRadius(16), BoxShadow = CardShadow, Padding = new Thickness(20)
        };
    }

    internal void Add(SurplusListing listing)
    {
        try
        {
            session.AddToCart(listing, 1);
            notice.Text = $"{listing.Menu.Name} ditambahkan ke keranjang.";
            notice.Foreground = Ui.Green;
        }
        catch (InvalidOperationException)
        {
            notice.Text = "Jumlah di keranjang sudah mencapai stok yang tersedia.";
            notice.Foreground = Brush.Parse("#9B3A25");
        }
        notice.IsVisible = true;
        Refresh();
    }
}
