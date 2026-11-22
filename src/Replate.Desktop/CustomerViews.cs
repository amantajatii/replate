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
        var quantity = Ui.Number("Masukkan jumlah porsi", 1, listing.Quantity); quantity.Minimum = 1;
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
            Ui.Text(session.RestaurantLine(listing.Menu.RestaurantId), 14, Ui.Muted),
            Ui.Text("Pesanan diambil langsung. Simpan kode reservasi untuk ditunjukkan kepada restoran.", 14, Ui.Muted),
            Ui.Field("Jumlah porsi", quantity), add);
        facts.Width = 680; facts.HorizontalAlignment = HorizontalAlignment.Left;
        Content = Ui.Page(Ui.Button("Kembali ke daftar makanan", host.ShowMarketplace), facts);
    }
}

public sealed partial class OrdersView : UserControl
{
    internal OrdersView(MainWindow host, DemoSession session, bool restaurant)
    {
        if (!restaurant) { BuildCustomer(host, session); return; }
        var filter = new ComboBox { ItemsSource = new[] { "Aktif", "Semua", "Selesai", "Dibatalkan" }, SelectedIndex = 0, FontSize = 15 };
        var list = Ui.Stack(0);
        void Refresh()
        {
            list.Children.Clear();
            var allOrders = session.RestaurantOrders.OrderByDescending(o => o.OrderedAt).ToList();
            var orders = allOrders.Where(o => filter.SelectedIndex switch {
                0 => o.Status is not (OrderStatus.Completed or OrderStatus.Cancelled),
                2 => o.Status == OrderStatus.Completed, 3 => o.Status == OrderStatus.Cancelled, _ => true }).ToList();
            if (orders.Count == 0)
            {
                if (allOrders.Count == 0)
                {
                    var create = Ui.Button("Buat penawaran", () => host.ShowListingEditor(), true);
                    create.HorizontalAlignment = HorizontalAlignment.Left;
                    list.Children.Add(Ui.Stack(12, Ui.Text("Belum ada pesanan", 20, bold: true),
                        Ui.Text("Pesanan muncul setelah pelanggan memesan penawaran surplus restoran ini.", 14, Ui.Muted), create));
                }
                else list.Children.Add(Ui.Text("Tidak ada pesanan pada filter ini.", 16, Ui.Muted));
                return;
            }
            foreach (var order in orders)
                list.Children.Add(Ui.TableRow("*,190,160,130", false,
                    Ui.Stack(6, Ui.Text($"#{order.Id.ToString("N")[..8].ToUpperInvariant()}", 16, bold: true),
                        Ui.Text(string.Join(", ", order.Items.Select(i => $"{i.Quantity} × {i.MenuName}")), 14),
                        Ui.Text(order.OrderedAt.ToLocalTime().ToString("dd MMM yyyy, HH:mm"), 12, Ui.Muted)),
                    new OrderStatusBadge(order),
                    Ui.Text(DemoSession.Rupiah(order.TotalPrice), 16, bold: true),
                    Ui.Button("Lihat pesanan", () => host.ShowOrder(order, true))));
        }
        filter.SelectionChanged += (_, _) => Refresh(); Refresh();
        var heading = Ui.Heading("Pesanan restoran", "Konfirmasi pesanan dan verifikasi pengambilan pelanggan.");
        var filterField = Ui.Form(Ui.Field("Status pesanan", filter), 320);
        Content = Ui.Page(heading, filterField, list);
    }
}

public sealed partial class OrderDetailView : UserControl
{
    internal Button? ConfirmButton { get; }
    internal Button? ReadyButton { get; }
    internal Button? VerifyButton { get; }
    internal TextBox? PickupInput { get; }
    internal Button? CopyCodeButton { get; private set; }
    internal TextBlock? PickupCodeText { get; private set; }
    internal OrderDetailView(MainWindow host, DemoSession session, Order order, bool restaurant)
    {
        if (!restaurant) { BuildCustomer(host, session, order); return; }
        var rows = Ui.Stack(0);
        foreach (var item in order.Items)
            rows.Children.Add(Ui.TableRow("*,Auto", false,
                Ui.Stack(6, Ui.Text($"{item.Quantity} × {item.MenuName}", 18, bold: true),
                    Ui.Text($"Ambil {item.Listing.PickupStart.ToLocalTime():dd MMM, HH:mm}–{item.Listing.PickupEnd.ToLocalTime():HH:mm}", 13, Ui.Muted)),
                Ui.Text(DemoSession.Rupiah(item.CalculateSubtotal()), 18, bold: true)));
        var actions = Ui.Stack(16);
        if (order.Status == OrderStatus.Reserved)
        {
            var minutes = Ui.Number("Masukkan estimasi menit", 15, 240); minutes.Minimum = 1;
            actions.Children.Add(Ui.Field("Estimasi persiapan (menit)", minutes));
            ConfirmButton = Ui.Button("Konfirmasi pesanan", () => host.Run(() => session.ConfirmOrder(order, (int)(minutes.Value ?? 15)), () => host.ShowOrder(order, true)), true);
            actions.Children.Add(ConfirmButton);
        }
        if (order.Status == OrderStatus.Confirmed)
        {
            ReadyButton = Ui.Button("Tandai siap diambil", () => host.Run(() => session.ReadyOrder(order), () => host.ShowOrder(order, true)), true);
            actions.Children.Add(ReadyButton);
        }
        if (order.Status == OrderStatus.ReadyForPickup)
        {
            PickupInput = Ui.Input("Masukkan kode pengambilan"); PickupInput.MaxLength = 6;
            actions.Children.Add(Ui.Field("Kode pengambilan dari pelanggan", PickupInput));
            VerifyButton = Ui.Button("Verifikasi dan selesaikan", () => host.Run(() => session.CompletePickup(order, PickupInput.Text ?? ""), () => host.ShowOrder(order, true)), true);
            actions.Children.Add(VerifyButton);
        }
        if (order.Status is not (OrderStatus.Completed or OrderStatus.Cancelled))
            actions.Children.Add(CancelButton(host, session, order, true));
        var status = Ui.Stack(8, new OrderStatusBadge(order), Ui.Text(StatusNote(order), 14, Ui.Muted));
        Content = Ui.Page(Ui.Button("Kembali ke pesanan", host.ShowRestaurantOrders),
            Ui.Heading($"Pesanan #{order.Id.ToString("N")[..8].ToUpperInvariant()}", order.OrderedAt.ToLocalTime().ToString("dd MMM yyyy, HH:mm")),
            status, Ui.Divider(), rows,
            Ui.Columns("*,Auto", Ui.Text("Total pesanan", 20, bold: true), Ui.Text(DemoSession.Rupiah(order.TotalPrice), 24, bold: true)),
            Ui.Text(session.RestaurantLine(order.RestaurantId), 14, Ui.Muted), actions);
    }

    internal static string StatusNote(Order order) => order.Status switch
    {
        OrderStatus.Reserved => "Restoran akan mengonfirmasi ketersediaan dan waktu persiapan.",
        OrderStatus.Confirmed => $"Estimasi siap {order.EstimatedReadyTime.ToLocalTime():dd MMM, HH:mm}.",
        OrderStatus.ReadyForPickup => "Pesanan sudah siap. Ambil sesuai jadwal yang tertera.",
        OrderStatus.Completed => "Pengambilan sudah diverifikasi oleh restoran.", _ => "Pesanan dibatalkan dan stok sudah dikembalikan."
    };

    private static Button CancelButton(MainWindow host, DemoSession session, Order order, bool restaurant) =>
        Ui.DangerButton("Batalkan pesanan", async () =>
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
            if (!restaurant) dialog.Styles.Add(Ui.FocusStyles());
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
        });
}
