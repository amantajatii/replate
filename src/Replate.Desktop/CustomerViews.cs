using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Replate.Domain.Entities;
using Replate.Domain.Enums;

namespace Replate.Desktop;

public sealed class FoodDetailView : UserControl
{
    internal FoodDetailView(MainWindow host, DemoSession session, SurplusListing listing)
    {
        var quantity = Ui.Number(1, listing.Quantity); quantity.Minimum = 1;
        var add = Ui.Button("Tambah ke keranjang", () => host.Run(() => session.AddToCart(listing, (int)(quantity.Value ?? 1)), host.ShowCart), true);
        add.IsEnabled = listing.IsAvailable(DateTime.UtcNow) && listing.Menu.IsActive;
        var facts = Ui.Stack(16,
            new MenuPhotoView(session.PhotoPath(listing.Menu.ImageUrl), 680, 200),
            Ui.Text(listing.Menu.Name, 32, bold: true), Ui.Text(listing.Menu.Category, 14, Ui.Muted),
            Ui.Text(listing.Menu.Description, 16), Ui.Divider(),
            Ui.Text(DemoSession.Rupiah(listing.RescuePrice), 26, Ui.Green, true),
            Ui.Text($"Harga normal {DemoSession.Rupiah(listing.Menu.NormalPrice)} · hemat {listing.CalculateDiscount():0}%", 14, Ui.Muted),
            Ui.Text($"Stok tersedia: {listing.Quantity} porsi", 14),
            Ui.Text($"Pengambilan {listing.PickupStart.ToLocalTime():dd MMM yyyy, HH:mm}–{listing.PickupEnd.ToLocalTime():HH:mm}", 14),
            Ui.Text($"{session.Restaurant.Name} · {session.Restaurant.Address}", 14, Ui.Muted),
            Ui.Text("Pesanan diambil langsung. Simpan kode reservasi untuk ditunjukkan kepada restoran.", 14, Ui.Muted),
            Ui.Field("Jumlah porsi", quantity), add);
        facts.Width = 680; facts.HorizontalAlignment = HorizontalAlignment.Left;
        Content = Ui.Page(Ui.Button("Kembali ke daftar makanan", host.ShowMarketplace), facts);
    }
}

public sealed class CartView : UserControl
{
    internal Button? CheckoutButton { get; private set; }
    internal CartView(MainWindow host, DemoSession session)
    {
        var items = Ui.Stack(0);
        foreach (var item in session.Cart.Items)
        {
            var minus = Ui.Button("−", () => host.Run(() => session.ChangeQuantity(item.ListingId, item.Quantity - 1), host.ShowCart));
            var plus = Ui.Button("+", () => host.Run(() => session.ChangeQuantity(item.ListingId, item.Quantity + 1), host.ShowCart));
            plus.IsEnabled = item.Quantity < item.Listing.Quantity && item.Listing.IsAvailable(DateTime.UtcNow);
            Avalonia.Automation.AutomationProperties.SetName(minus, $"Kurangi jumlah {item.Listing.Menu.Name}");
            Avalonia.Automation.AutomationProperties.SetName(plus, $"Tambah jumlah {item.Listing.Menu.Name}");
            var information = Ui.Stack(6, Ui.Text(item.Listing.Menu.Name, 18, bold: true),
                Ui.Text($"Pengambilan {item.Listing.PickupStart.ToLocalTime():dd MMM, HH:mm}–{item.Listing.PickupEnd.ToLocalTime():HH:mm}", 13, Ui.Muted),
                    Ui.Text(item.Listing.IsAvailable(DateTime.UtcNow) && item.Listing.Menu.IsActive ? $"{DemoSession.Rupiah(item.UnitPrice)} / porsi" : "Penawaran tidak tersedia. Hapus untuk melanjutkan.", 13, Ui.Muted));
            items.Children.Add(Ui.TableRow("*,160,160,100", false, information,
                Ui.Row(8, minus, Ui.Text(item.Quantity.ToString()), plus),
                Ui.Text(DemoSession.Rupiah(item.CalculateSubtotal()), 18, bold: true),
                Ui.Button("Hapus", () => host.Run(() => session.ChangeQuantity(item.ListingId, 0), host.ShowCart))));
        }
        if (session.Cart.Items.Count == 0)
        {
            Content = Ui.Page(Ui.Heading("Keranjang", "Belum ada makanan yang dipilih."),
                Ui.Button("Jelajahi makanan", host.ShowMarketplace, true)); return;
        }
        CheckoutButton = Ui.Button("Reservasi pesanan", () => host.Run(() => session.Checkout(), () => host.ShowOrder(session.LastOrder!, false)), true);
        CheckoutButton.IsEnabled = session.Cart.ValidateCart() && session.Cart.Items.All(i => i.Listing.Menu.IsActive);
        Content = Ui.Page(Ui.Heading("Keranjang", $"{session.Restaurant.Name} · {session.Restaurant.Address}"), items,
            Ui.Columns("*,Auto", Ui.Text("Total pesanan", 20, bold: true), Ui.Text(DemoSession.Rupiah(session.Cart.CalculateSubtotal()), 26, Ui.Green, true)),
            Ui.Text("Periksa jumlah dan jadwal sebelum reservasi. Pembayaran belum tersedia pada prototipe ini.", 14, Ui.Muted),
            Ui.Row(16, CheckoutButton, Ui.Button("Tambah makanan lain", host.ShowMarketplace)));
    }
}

public sealed class OrdersView : UserControl
{
    internal OrdersView(MainWindow host, DemoSession session, bool restaurant)
    {
        var filter = new ComboBox { ItemsSource = new[] { "Aktif", "Semua", "Selesai", "Dibatalkan" }, SelectedIndex = 0, FontSize = 15 };
        var list = Ui.Stack(0);
        var results = new ContentControl
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch
        };
        var scroll = restaurant ? null : Ui.Scroll(list);
        void Refresh()
        {
            list.Children.Clear();
            var source = restaurant ? session.Restaurant.Orders.OrderByDescending(o => o.OrderedAt) : session.CustomerOrders;
            var allOrders = source.ToList();
            var orders = allOrders.Where(o => filter.SelectedIndex switch {
                0 => o.Status is not (OrderStatus.Completed or OrderStatus.Cancelled),
                2 => o.Status == OrderStatus.Completed, 3 => o.Status == OrderStatus.Cancelled, _ => true }).ToList();
            if (orders.Count == 0)
            {
                if (restaurant) list.Children.Add(Ui.Text("Tidak ada pesanan pada filter ini.", 16, Ui.Muted));
                else
                {
                    var noHistory = allOrders.Count == 0;
                    var title = noHistory ? "Belum ada pesanan" : filter.SelectedIndex switch
                    {
                        0 => "Tidak ada pesanan aktif saat ini.",
                        2 => "Belum ada pesanan selesai.",
                        3 => "Tidak ada pesanan yang dibatalkan.",
                        _ => "Tidak ada pesanan pada filter ini."
                    };
                    results.Content = EmptyOrders(title,
                        noHistory ? "Cari makanan yang kamu sukai, yuk!" : "Ubah filter untuk melihat pesanan lainnya.",
                        noHistory ? "Jelajahi Makanan" : "Lihat Semua Pesanan",
                        noHistory ? host.ShowMarketplace : () => filter.SelectedIndex = 1);
                }
                return;
            }
            if (!restaurant) results.Content = scroll;
            foreach (var order in orders)
                list.Children.Add(Ui.TableRow("*,190,160,130", false,
                    Ui.Stack(6, Ui.Text($"#{order.Id.ToString("N")[..8].ToUpperInvariant()}", 16, bold: true),
                        Ui.Text(string.Join(", ", order.Items.Select(i => $"{i.Quantity} × {i.MenuName}")), 14),
                        Ui.Text(order.OrderedAt.ToLocalTime().ToString("dd MMM yyyy, HH:mm"), 12, Ui.Muted)),
                    new OrderStatusBadge(order),
                    Ui.Text(DemoSession.Rupiah(order.TotalPrice), 16, bold: true),
                    Ui.Button("Lihat pesanan", () => host.ShowOrder(order, restaurant))));
        }
        filter.SelectionChanged += (_, _) => Refresh(); Refresh();
        var heading = Ui.Heading(restaurant ? "Pesanan restoran" : "Pesanan saya",
            restaurant ? "Konfirmasi pesanan dan verifikasi pengambilan pelanggan." : "Pantau reservasi dan simpan kode pengambilan.");
        var filterField = Ui.Form(Ui.Field("Status pesanan", filter), 320);
        if (restaurant) Content = Ui.Page(heading, filterField, list);
        else
        {
            var page = new Grid { RowDefinitions = RowDefinitions.Parse("Auto,*"), RowSpacing = 24, Margin = new Thickness(32) };
            page.Children.Add(Ui.Stack(24, heading, filterField));
            Grid.SetRow(results, 1); page.Children.Add(results);
            Content = page;
        }
    }
    private static Control EmptyOrders(string title, string description, string actionLabel, Action action)
    {
        var icon = new Border
        {
            Width = 64, Height = 64, CornerRadius = new CornerRadius(16),
            Background = Brush.Parse("#E0E9DF"), HorizontalAlignment = HorizontalAlignment.Center,
            Child = new Avalonia.Controls.Shapes.Path
            {
                Width = 30, Height = 34, Stretch = Stretch.Uniform,
                Stroke = Ui.Green, StrokeThickness = 1.8,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                Data = Geometry.Parse("M6 3H18V21L15 19L12 21L9 19L6 21Z M9 7H15 M9 11H15 M9 15H13")
            }
        };
        var heading = Ui.Text(title, 22, bold: true);
        heading.TextAlignment = TextAlignment.Center;
        var subtitle = Ui.Text(description, 15, Ui.Muted);
        subtitle.TextAlignment = TextAlignment.Center;
        var button = Ui.Button(actionLabel, action, true);
        button.FontSize = 14;
        button.HorizontalAlignment = HorizontalAlignment.Center;
        var body = Ui.Stack(16, icon, Ui.Stack(8, heading, subtitle), button);
        body.MaxWidth = 480; body.Margin = new Thickness(16);
        body.HorizontalAlignment = HorizontalAlignment.Center;
        body.VerticalAlignment = VerticalAlignment.Center;
        return body;
    }
}

public sealed class OrderDetailView : UserControl
{
    internal Button? ConfirmButton { get; }
    internal Button? ReadyButton { get; }
    internal Button? VerifyButton { get; }
    internal TextBox? PickupInput { get; }
    internal Button? CopyCodeButton { get; }
    internal TextBlock? PickupCodeText { get; }
    internal OrderDetailView(MainWindow host, DemoSession session, Order order, bool restaurant)
    {
        var rows = Ui.Stack(0);
        foreach (var item in order.Items)
            rows.Children.Add(Ui.TableRow("*,Auto", false,
                Ui.Stack(6, Ui.Text($"{item.Quantity} × {item.MenuName}", 18, bold: true),
                    Ui.Text($"Ambil {item.Listing.PickupStart.ToLocalTime():dd MMM, HH:mm}–{item.Listing.PickupEnd.ToLocalTime():HH:mm}", 13, Ui.Muted)),
                Ui.Text(DemoSession.Rupiah(item.CalculateSubtotal()), 18, bold: true)));
        var actions = Ui.Stack(16);
        if (!restaurant && order.Status != OrderStatus.Cancelled)
        {
            actions.Children.Add(Ui.Text("Kode pengambilan", 14, Ui.Muted));
            var pickupCode = order.PickupCode;
            PickupCodeText = Ui.Text(pickupCode, 36, Ui.Green, true);
            if (!string.IsNullOrWhiteSpace(pickupCode))
            {
                CopyCodeButton = Ui.Button("", async () =>
                {
                    CopyCodeButton!.IsEnabled = false;
                    try
                    {
                        await host.RunAsync(async () =>
                        {
                            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard
                                ?? throw new InvalidOperationException("Clipboard unavailable");
                            await clipboard.SetTextAsync(pickupCode);
                        }, "Kode berhasil disalin.", "Kode belum berhasil disalin. Silakan coba lagi.");
                    }
                    finally { CopyCodeButton!.IsEnabled = true; }
                });
                CopyCodeButton.Content = new PathIcon
                {
                    Width = 18, Height = 18, Foreground = Ui.Green,
                    Data = Geometry.Parse("M8 2H20V18H16V22H2V6H8V2ZM10 4V6H16V16H18V4H10ZM4 8V20H14V8H4Z")
                };
                CopyCodeButton.Width = CopyCodeButton.Height = 44;
                CopyCodeButton.Padding = new Thickness(12);
                CopyCodeButton.Focusable = true;
                ToolTip.SetTip(CopyCodeButton, "Salin kode pengambilan");
                Avalonia.Automation.AutomationProperties.SetName(CopyCodeButton, "Salin kode pengambilan");
                actions.Children.Add(Ui.Row(12, PickupCodeText, CopyCodeButton));
            }
            else actions.Children.Add(PickupCodeText);
            actions.Children.Add(Ui.Text("Tunjukkan kode ini saat pesanan siap diambil.", 14, Ui.Muted));
        }
        if (restaurant && order.Status == OrderStatus.Reserved)
        {
            var minutes = Ui.Number(15, 240); minutes.Minimum = 1;
            actions.Children.Add(Ui.Field("Estimasi persiapan (menit)", minutes));
            ConfirmButton = Ui.Button("Konfirmasi pesanan", () => host.Run(() => session.ConfirmOrder(order, (int)(minutes.Value ?? 15)), () => host.ShowOrder(order, true)), true);
            actions.Children.Add(ConfirmButton);
        }
        if (restaurant && order.Status == OrderStatus.Confirmed)
        {
            ReadyButton = Ui.Button("Tandai siap diambil", () => host.Run(() => session.ReadyOrder(order), () => host.ShowOrder(order, true)), true);
            actions.Children.Add(ReadyButton);
        }
        if (restaurant && order.Status == OrderStatus.ReadyForPickup)
        {
            PickupInput = Ui.Input(); PickupInput.MaxLength = 6;
            actions.Children.Add(Ui.Field("Kode pengambilan dari pelanggan", PickupInput));
            VerifyButton = Ui.Button("Verifikasi dan selesaikan", () => host.Run(() => session.CompletePickup(order, PickupInput.Text ?? ""), () => host.ShowOrder(order, true)), true);
            actions.Children.Add(VerifyButton);
        }
        if (order.Status is not (OrderStatus.Completed or OrderStatus.Cancelled))
        {
            actions.Children.Add(Ui.Button("Batalkan pesanan", async () =>
            {
                var dialog = new Window
                {
                    Title = "Batalkan Pesanan",
                    Width = 520,
                    SizeToContent = SizeToContent.Height,
                    CanResize = false,
                    CanMinimize = false,
                    CanMaximize = false,
                    ShowInTaskbar = false,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Background = host.Background,
                    FontFamily = host.FontFamily
                };
                var dismiss = Ui.Button("Kembali", () => dialog.Close(false));
                var confirm = Ui.Button("Ya, batalkan pesanan", () => dialog.Close(true), true);
                var buttons = Ui.Row(12, dismiss, confirm);
                buttons.HorizontalAlignment = HorizontalAlignment.Right;
                var body = Ui.Stack(20,
                    Ui.Text("Batalkan Pesanan", 24, bold: true),
                    Ui.Text("Apakah Anda setuju untuk melakukan pembatalan pesanan ini?"),
                    buttons);
                body.Margin = new Thickness(24);
                dialog.Content = body;
                dialog.Opened += (_, _) => dismiss.Focus();
                dialog.AddHandler(Avalonia.Input.InputElement.KeyDownEvent, (_, e) =>
                {
                    if (e.Key != Avalonia.Input.Key.Escape) return;
                    e.Handled = true;
                    dialog.Close(false);
                }, Avalonia.Interactivity.RoutingStrategies.Tunnel);
                if (await dialog.ShowDialog<bool>(host))
                    host.Run(() => session.CancelOrder(order), () => host.ShowOrder(order, restaurant), "Pesanan berhasil dibatalkan.");
            }));
        }
        var status = Ui.Stack(8, new OrderStatusBadge(order),
            Ui.Text(order.Status switch {
                OrderStatus.Reserved => "Restoran akan mengonfirmasi ketersediaan dan waktu persiapan.",
                OrderStatus.Confirmed => $"Estimasi siap {order.EstimatedReadyTime.ToLocalTime():dd MMM, HH:mm}.",
                OrderStatus.ReadyForPickup => "Pesanan sudah siap. Ambil sesuai jadwal yang tertera.",
                OrderStatus.Completed => "Pengambilan sudah diverifikasi oleh restoran.", _ => "Pesanan dibatalkan dan stok sudah dikembalikan." }, 14, Ui.Muted));
        Content = Ui.Page(Ui.Button("Kembali ke pesanan", () => { if (restaurant) host.ShowRestaurantOrders(); else host.ShowCustomerOrders(); }),
            Ui.Heading($"Pesanan #{order.Id.ToString("N")[..8].ToUpperInvariant()}", order.OrderedAt.ToLocalTime().ToString("dd MMM yyyy, HH:mm")),
            status, Ui.Divider(), rows,
            Ui.Columns("*,Auto", Ui.Text("Total pesanan", 20, bold: true), Ui.Text(DemoSession.Rupiah(order.TotalPrice), 24, bold: true)),
            Ui.Text($"{session.Restaurant.Name} · {session.Restaurant.Address}", 14, Ui.Muted), actions);
    }
}
