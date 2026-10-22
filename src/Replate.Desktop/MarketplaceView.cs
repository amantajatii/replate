using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Replate.Domain.Entities;

namespace Replate.Desktop;

public sealed class MarketplaceView : UserControl
{
    private readonly MainWindow owner;
    private readonly DemoSession session;
    internal TextBox SearchBox { get; } = new() { PlaceholderText = "Cari makanan atau restoran…", Height = 44,
        CornerRadius = new CornerRadius(6), Background = Brushes.White, PlaceholderForeground = Ui.Muted };
    private readonly StackPanel listingsPanel = Ui.Stack(0);
    private readonly TextBlock resultLabel = Ui.Text("", 13, Ui.Muted);
    private readonly TextBlock notice = Ui.Text("", 12, Ui.Green);
    private readonly Dictionary<string, Button> categories = [];
    internal int VisibleCount { get; private set; }
    private string category = "Semua";

    internal MarketplaceView(MainWindow owner, DemoSession session)
    {
        this.owner = owner; this.session = session;
        var top = Ui.Heading("Makanan tersedia", "Makanan surplus dengan harga khusus. Ambil langsung di restoran.");
        Avalonia.Automation.AutomationProperties.SetName(SearchBox, "Cari makanan atau restoran");
        var filterRow = Ui.Row(4);
        foreach (var name in new[] { "Semua", "Makanan utama", "Roti & kue", "Vegetarian" })
        {
            var button = Ui.Button(name, () => SelectCategory(name));
            categories.Add(name, button); filterRow.Children.Add(button);
        }
        SearchBox.TextChanged += (_, _) => Refresh();
        var search = Ui.Stack(8, Ui.Text("Cari makanan atau restoran", 13, bold: true), SearchBox, filterRow);
        var section = Ui.Columns("*,Auto", Ui.Stack(4, Ui.Text(session.Restaurant.Name, 20, bold: true),
            Ui.Text(session.Restaurant.Address, 13, Ui.Muted)), resultLabel);
        section.Margin = new Thickness(0, 8, 0, 0);
        notice.IsVisible = false;
        var body = Ui.Stack(24, top, search, section, notice, listingsPanel,
            Ui.Text("Harga dan stok contoh. Reservasi tidak melibatkan pembayaran.", 12, Ui.Muted));
        body.Margin = new Thickness(32);
        Content = new ScrollViewer { Content = body, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
    }

    internal void SelectCategory(string value)
    {
        category = value; Refresh();
    }

    internal void Refresh()
    {
        var listings = session.Search(SearchBox.Text?.Trim() ?? "", category).ToList();
        VisibleCount = listings.Count;
        resultLabel.Text = $"{VisibleCount} menu tersedia";
        owner.UpdateCartCount();
        if (session.Cart.Items.Count == 0) notice.IsVisible = false;
        foreach (var (name, button) in categories)
        {
            button.Background = name == category ? Brush.Parse("#E0E9DF") : Brushes.Transparent;
            button.Foreground = name == category ? Ui.Green : Ui.Muted;
        }
        listingsPanel.Children.Clear();
        foreach (var listing in listings) listingsPanel.Children.Add(FoodRow(listing));
        if (listings.Count == 0)
            listingsPanel.Children.Add(Ui.Stack(12, Ui.Text("Tidak ada menu yang cocok", 20, bold: true),
                Ui.Text("Coba kata kunci lain atau pilih kategori Semua.", 13, Ui.Muted),
                Ui.Button("Hapus filter", () => { category = "Semua"; SearchBox.Text = ""; Refresh(); })));
    }

    private Control FoodRow(SurplusListing listing)
    {
        var price = Ui.Stack(5, Ui.Text(DemoSession.Rupiah(listing.RescuePrice), 18, bold: true),
            new TextBlock { Text = DemoSession.Rupiah(listing.Menu.NormalPrice), FontSize = 12,
                Foreground = Ui.Muted, TextDecorations = TextDecorations.Strikethrough },
            Ui.Text($"Hemat {listing.CalculateDiscount():0}%", 12, Ui.Green));
        var add = Ui.Button("Tambah", () => Add(listing), true);
        Avalonia.Automation.AutomationProperties.SetName(add, $"Tambah {listing.Menu.Name} ke keranjang");
        var details = Ui.Button(listing.Menu.Name, () => owner.ShowFood(listing));
        details.Background = Brushes.Transparent; details.Padding = new Thickness(0); details.FontSize = 18;
        var description = Ui.Stack(6, details,
            Ui.Text(listing.Menu.Description, 13, Ui.Muted), Ui.Text(listing.Menu.Category, 12, Ui.Muted));
        description.Margin = new Thickness(0, 0, 24, 0);
        var pickup = Ui.Stack(5, Ui.Text("Pengambilan", 12, Ui.Muted),
            Ui.Text($"{listing.PickupStart.ToLocalTime():HH:mm}–{listing.PickupEnd.ToLocalTime():HH:mm}", 14),
            Ui.Text($"{listing.Quantity} porsi tersisa", 12, Ui.Muted));
        pickup.VerticalAlignment = price.VerticalAlignment = add.VerticalAlignment = VerticalAlignment.Center;
        return new Border { Child = Ui.Columns("*,136,120,90", description, pickup, price, add),
            Padding = new Thickness(0, 22), BorderBrush = Brush.Parse("#DDE3DA"), BorderThickness = new Thickness(0, 0, 0, 1) };
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
