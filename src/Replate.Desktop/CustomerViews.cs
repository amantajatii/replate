using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
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
        var filter = new ComboBox { ItemsSource = new[] { "Aktif", "Semua", "Selesai", "Dibatalkan" }, SelectedIndex = 0, MinWidth = 180 };
        var list = Ui.Stack(0);
        void Refresh()
        {
            list.Children.Clear();
            var source = restaurant ? session.Restaurant.Orders.OrderByDescending(o => o.OrderedAt) : session.CustomerOrders;
            var orders = source.Where(o => filter.SelectedIndex switch {
                0 => o.Status is not (OrderStatus.Completed or OrderStatus.Cancelled),
                2 => o.Status == OrderStatus.Completed, 3 => o.Status == OrderStatus.Cancelled, _ => true }).ToList();
            if (orders.Count == 0)
            {
                list.Children.Add(Ui.Text("Tidak ada pesanan pada filter ini.", 16, Ui.Muted));
                if (!restaurant) list.Children.Add(Ui.Button("Jelajahi makanan", host.ShowMarketplace));
                return;
            }
            foreach (var order in orders)
                list.Children.Add(Ui.TableRow("*,190,160,130", false,
                    Ui.Stack(6, Ui.Text($"#{order.Id.ToString("N")[..8].ToUpperInvariant()}", 16, bold: true),
                        Ui.Text(string.Join(", ", order.Items.Select(i => $"{i.Quantity} × {i.MenuName}")), 14),
                        Ui.Text(order.OrderedAt.ToLocalTime().ToString("dd MMM yyyy, HH:mm"), 12, Ui.Muted)),
                    Ui.Text(DemoSession.Status(order), 14, order.Status == OrderStatus.ReadyForPickup ? Ui.Green : Ui.Ink),
                    Ui.Text(DemoSession.Rupiah(order.TotalPrice), 16, bold: true),
                    Ui.Button("Lihat pesanan", () => host.ShowOrder(order, restaurant))));
        }
        filter.SelectionChanged += (_, _) => Refresh(); Refresh();
        Content = Ui.Page(Ui.Heading(restaurant ? "Pesanan restoran" : "Pesanan saya",
            restaurant ? "Konfirmasi pesanan dan verifikasi pengambilan pelanggan." : "Pantau reservasi dan simpan kode pengambilan."),
            Ui.Field("Status pesanan", filter), list);
    }
}

public sealed class OrderDetailView : UserControl
{
    internal Button? ConfirmButton { get; }
    internal Button? ReadyButton { get; }
    internal Button? VerifyButton { get; }
    internal TextBox? PickupInput { get; }
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
            actions.Children.Add(Ui.Text(order.PickupCode, 36, Ui.Green, true));
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
            var confirmation = Ui.Stack(12, Ui.Text("Batalkan pesanan ini? Stok akan dikembalikan ke penawaran.", 14),
                Ui.Button("Ya, batalkan pesanan", () => host.Run(() => session.CancelOrder(order), () => host.ShowOrder(order, restaurant))));
            confirmation.IsVisible = false;
            actions.Children.Add(Ui.Button("Batalkan pesanan", () => confirmation.IsVisible = !confirmation.IsVisible));
            actions.Children.Add(confirmation);
        }
        var status = Ui.Stack(8, Ui.Text(DemoSession.Status(order), 20, order.Status == OrderStatus.ReadyForPickup ? Ui.Green : Ui.Ink, true),
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
