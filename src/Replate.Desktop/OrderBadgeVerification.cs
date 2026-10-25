using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using Replate.Domain.Entities;
using Replate.Domain.Enums;

namespace Replate.Desktop;

internal static class OrderBadgeVerification
{
    internal static async Task Run(string output)
    {
        void Ensure(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
        void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        async Task Settle() => await Task.Delay(250);
        var expected = new Dictionary<OrderStatus, (string Label, string Color)>
        {
            [OrderStatus.Reserved] = ("Menunggu Konfirmasi", "#FFF3CD"),
            [OrderStatus.Confirmed] = ("Diproses", "#DBEAFE"),
            [OrderStatus.ReadyForPickup] = ("Siap Diambil", "#EDE0FA"),
            [OrderStatus.Completed] = ("Selesai", "#E0E9DF"),
            [OrderStatus.Cancelled] = ("Dibatalkan", "#E8EAED")
        };
        void CheckBadge(OrderStatusBadge badge, OrderStatus status)
        {
            var label = (TextBlock)badge.Child!;
            Ensure(badge.Status == status && label.Text == expected[status].Label, "Badge matches actual order status and requested label");
            Ensure(((ISolidColorBrush)badge.Background!).Color == Color.Parse(expected[status].Color) &&
                ((ISolidColorBrush)label.Foreground!).Color == Color.Parse("#23382C"), "Badge color and dark text");
            Ensure(badge.CornerRadius == new CornerRadius(6) && badge.Padding == new Thickness(10, 5), "Consistent badge shape");
        }
        void CheckDetail(MainWindow window, Order order)
        {
            CheckBadge(window.GetVisualDescendants().OfType<OrderStatusBadge>().Single(), order.Status);
        }
        void Shot(MainWindow window, string name)
        {
            using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height), new Vector(96, 96));
            bitmap.Render(window); bitmap.Save(Path.Combine(output, name + ".png"), PngBitmapEncoderOptions.Default);
        }
        var path = Path.Combine(Path.GetTempPath(), "replate-badges-" + Guid.NewGuid().ToString("N"), "state.json");
        var session = new DemoSession(path);
        session.Restaurant.Orders.Clear(); // Only the isolated test session's generated history.
        session.Persist();
        var listing = session.Listings[0];
        var orders = new List<Order>();
        MainWindow window = new(session);
        try
        {
            window.Show(); await Settle();
            foreach (var target in expected.Keys)
            {
                session.AddToCart(listing, 1);
                var order = session.Checkout(); orders.Add(order);
                window.ShowOrder(order, true); await Settle(); CheckDetail(window, order);
                if (target is OrderStatus.Confirmed or OrderStatus.ReadyForPickup or OrderStatus.Completed)
                {
                    Click(((OrderDetailView)window.CurrentPage!).ConfirmButton!); await Settle();
                    Ensure(order.Status == OrderStatus.Confirmed, "Restaurant confirmation transition"); CheckDetail(window, order);
                }
                if (target is OrderStatus.ReadyForPickup or OrderStatus.Completed)
                {
                    Click(((OrderDetailView)window.CurrentPage!).ReadyButton!); await Settle();
                    Ensure(order.Status == OrderStatus.ReadyForPickup, "Ready transition"); CheckDetail(window, order);
                }
                if (target == OrderStatus.Completed)
                {
                    var detail = (OrderDetailView)window.CurrentPage!;
                    detail.PickupInput!.Text = "WRONG"; Click(detail.VerifyButton!); await Settle();
                    Ensure(order.Status == OrderStatus.ReadyForPickup, "Invalid pickup leaves badge unchanged"); CheckDetail(window, order);
                    detail.PickupInput.Text = order.PickupCode; Click(detail.VerifyButton!); await Settle();
                    Ensure(order.Status == OrderStatus.Completed, "Successful pickup transition"); CheckDetail(window, order);
                }
                if (target == OrderStatus.Cancelled)
                {
                    var cancel = window.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == "Batalkan pesanan");
                    Click(cancel); await Settle();
                    var dialog = window.OwnedWindows.Single();
                    Click(dialog.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == "Ya, batalkan pesanan")); await Settle();
                    Ensure(order.Status == OrderStatus.Cancelled, "Successful cancellation transition"); CheckDetail(window, order);
                }
            }
            Ensure(listing.Quantity == 2, "Five reservations and one cancellation preserve stock rules");
            var ids = orders.ToDictionary(o => o.Id, o => o.Status);
            window.Close();
            session = new DemoSession(path);
            Ensure(session.StorageError is null && session.CustomerOrders.Count() == 5 && session.Restaurant.Orders.All(o => ids[o.Id] == o.Status),
                "All five order statuses survive reload");
            window = new MainWindow(session); window.Show(); await Settle();
            foreach (var restaurant in new[] { false, true })
            {
                var area = restaurant ? "restaurant" : "customer";
                if (restaurant) window.ShowRestaurantOrders(); else window.ShowCustomerOrders();
                await Settle();
                var filter = window.GetVisualDescendants().OfType<ComboBox>().Single();
                Ensure(window.GetVisualDescendants().OfType<OrderStatusBadge>().Count() == 3, "Active filter preserved");
                filter.SelectedIndex = 1; await Settle();
                var badges = window.GetVisualDescendants().OfType<OrderStatusBadge>().ToList();
                Ensure(badges.Count == 5, "All five badges present in order list");
                foreach (var status in expected.Keys) CheckBadge(badges.Single(b => b.Status == status), status);
                Shot(window, $"badges-{area}-list-all");
                filter.SelectedIndex = 2; await Settle();
                CheckBadge(window.GetVisualDescendants().OfType<OrderStatusBadge>().Single(), OrderStatus.Completed);
                filter.SelectedIndex = 3; await Settle();
                CheckBadge(window.GetVisualDescendants().OfType<OrderStatusBadge>().Single(), OrderStatus.Cancelled);
                foreach (var order in session.CustomerOrders)
                {
                    window.ShowOrder(order, restaurant); await Settle(); CheckDetail(window, order);
                    var detail = (OrderDetailView)window.CurrentPage!;
                    Ensure((detail.ConfirmButton is not null) == (restaurant && order.Status == OrderStatus.Reserved) &&
                        (detail.ReadyButton is not null) == (restaurant && order.Status == OrderStatus.Confirmed) &&
                        (detail.VerifyButton is not null) == (restaurant && order.Status == OrderStatus.ReadyForPickup), "Status actions preserved in both areas");
                    var canCancel = window.GetVisualDescendants().OfType<Button>().Any(b => b.Content as string == "Batalkan pesanan");
                    Ensure(canCancel == (order.Status is not (OrderStatus.Completed or OrderStatus.Cancelled)), "Cancellation availability preserved");
                    Shot(window, $"badges-{area}-detail-{order.Status}");
                }
            }
            Console.WriteLine("All Replate order badge checks passed (five statuses, both areas, lists/details, transitions, filters, reload).");
        }
        finally { window.Close(); }
    }
}