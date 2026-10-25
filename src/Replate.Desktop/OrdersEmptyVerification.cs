using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using Replate.Domain.Enums;

namespace Replate.Desktop;

internal static class OrdersEmptyVerification
{
    internal static async Task Run(string output)
    {
        void Ensure(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
        void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        async Task Settle() => await Task.Delay(300);
        var session = new DemoSession();
        Ensure(session.Restaurant.Orders.Count > 0 && !session.CustomerOrders.Any(), "Other customers' history does not count as current customer's history");
        var window = new MainWindow(session) { Width = 1040, Height = 720 };
        try
        {
            window.Show(); await Settle();
            IEnumerable<TextBlock> Texts() => window.CurrentPage!.GetVisualDescendants().OfType<TextBlock>();
            IEnumerable<Button> Buttons() => window.CurrentPage!.GetVisualDescendants().OfType<Button>();
            ComboBox Filter() => window.CurrentPage!.GetVisualDescendants().OfType<ComboBox>().Single();
            void Shot(string name)
            {
                using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height), new Vector(96, 96));
                bitmap.Render(window); bitmap.Save(Path.Combine(output, name + ".png"), PngBitmapEncoderOptions.Default);
            }
            void CheckEmpty(string title, string action)
            {
                Ensure(Texts().Count(t => t.Text == title) == 1 && Buttons().Count(b => b.Content as string == action) == 1,
                    "One empty-state message and action");
                Ensure(!Texts().Any(t => t.Text == "Tidak ada pesanan pada filter ini.") &&
                    !window.CurrentPage!.GetVisualDescendants().OfType<OrderStatusBadge>().Any(), "Old empty state and order rows are absent");
                var button = Buttons().Single(b => b.Content as string == action);
                Ensure(((ISolidColorBrush)button.Background!).Color == Color.Parse("#245B43"), "Empty-state action uses primary green");
                var group = button.GetVisualAncestors().OfType<StackPanel>().First(p => p.MaxWidth == 480);
                var area = group.GetVisualAncestors().OfType<ContentControl>().First();
                var center = group.TranslatePoint(new Point(group.Bounds.Width / 2, group.Bounds.Height / 2), area)!.Value;
                Ensure(Math.Abs(center.X - area.Bounds.Width / 2) < 2 && Math.Abs(center.Y - area.Bounds.Height / 2) < 2,
                    "Empty-state group is centered in the results area below the filter");
                var corner = group.TranslatePoint(default, window)!.Value;
                Ensure(corner.X >= 0 && corner.Y >= 0 && corner.X + group.Bounds.Width <= window.Bounds.Width &&
                    corner.Y + group.Bounds.Height <= window.Bounds.Height, "Empty state fits supported window");
                Ensure(group.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>().Any(), "Order icon is shown");
            }
            void CheckResults(OrderStatus status, int count)
            {
                var badges = window.CurrentPage!.GetVisualDescendants().OfType<OrderStatusBadge>().ToList();
                Ensure(badges.Count == count && badges.All(b => b.Status == status), "Filtered results preserve order badges");
                Ensure(!Buttons().Any(b => b.Content as string is "Lihat Semua Pesanan" or "Jelajahi Makanan"), "Empty-state actions disappear when results exist");
            }
            foreach (var maximized in new[] { false, true })
            {
                window.WindowState = maximized ? WindowState.Maximized : WindowState.Normal; await Settle();
                window.ShowCustomerOrders(); await Settle();
                var filterPoint = Filter().TranslatePoint(default, window)!.Value;
                for (var index = 0; index < 4; index++)
                {
                    Filter().SelectedIndex = index; await Settle();
                    CheckEmpty("Belum ada pesanan", "Jelajahi Makanan");
                    Ensure(Texts().Any(t => t.Text == "Cari makanan yang kamu sukai, yuk!") &&
                        Filter().TranslatePoint(default, window)!.Value == filterPoint, "No-history description and fixed filter position");
                }
                Shot($"orders-empty-no-history-{(maximized ? "maximized" : "minimum")}");
            }
            Click(Buttons().Single(b => b.Content as string == "Jelajahi Makanan"));
            Ensure(window.CurrentPage is MarketplaceView, "Explore action opens marketplace");
            session.AddToCart(session.Listings[0], 1);
            var order = session.Checkout();
            window.WindowState = WindowState.Normal; window.Width = 1040; window.Height = 720;
            window.ShowCustomerOrders(); await Settle(); CheckResults(OrderStatus.Reserved, 1);
            foreach (var (index, message) in new[] { (2, "Belum ada pesanan selesai."), (3, "Tidak ada pesanan yang dibatalkan.") })
            {
                Filter().SelectedIndex = index; await Settle();
                CheckEmpty(message, "Lihat Semua Pesanan");
                Shot($"orders-empty-filter-{index}");
                Click(Buttons().Single(b => b.Content as string == "Lihat Semua Pesanan")); await Settle();
                Ensure(Filter().SelectedIndex == 1, "Show all selects existing Semua filter"); CheckResults(OrderStatus.Reserved, 1);
            }
            Click(Buttons().Single(b => b.Content as string == "Lihat pesanan"));
            Ensure(window.CurrentPage is OrderDetailView, "Order detail navigation preserved");
            session.ConfirmOrder(order, 15); session.ReadyOrder(order); session.CompletePickup(order, order.PickupCode);
            window.ShowCustomerOrders(); await Settle();
            CheckEmpty("Tidak ada pesanan aktif saat ini.", "Lihat Semua Pesanan");
            Shot("orders-empty-active-history");
            Click(Buttons().Single(b => b.Content as string == "Lihat Semua Pesanan")); await Settle(); CheckResults(OrderStatus.Completed, 1);
            Filter().SelectedIndex = 2; await Settle(); CheckResults(OrderStatus.Completed, 1);
            Shot("orders-empty-completed-results");
            session.AddToCart(session.Listings[0], 1);
            var cancelled = session.Checkout(); session.CancelOrder(cancelled);
            window.ShowCustomerOrders(); await Settle(); Filter().SelectedIndex = 3; await Settle(); CheckResults(OrderStatus.Cancelled, 1);
            Console.WriteLine("All Replate customer order empty-state checks passed (no history, other customers, empty filters, show all, results/badges/navigation, minimum/maximized).");
        }
        finally { window.Close(); }
    }
}