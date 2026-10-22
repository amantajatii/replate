using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace Replate.Desktop;

internal static class UiVerification
{
    public static async Task Run(MainWindow window, IClassicDesktopStyleApplicationLifetime desktop)
    {
        try
        {
            var args = desktop.Args!;
            var position = Array.IndexOf(args, "--verify-ui");
            var output = position + 1 < args.Length ? args[position + 1] : Path.GetTempPath();
            Directory.CreateDirectory(output);
            async Task Shot(string name) { await Settle(); Capture(window, Path.Combine(output, name + ".png")); }
            void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Shot("entry");
            window.ShowMarketplace(); await Shot("marketplace");
            var market = window.Marketplace;
            Ensure(market.VisibleCount == 4, "Initial listings");
            market.SearchBox.Text = "croissant"; await Settle(); Ensure(market.VisibleCount == 1, "Search");
            market.SelectCategory("Vegetarian"); Ensure(market.VisibleCount == 0, "Combined empty filter");
            await Shot("marketplace-empty");
            market.SearchBox.Text = ""; await Settle(); Ensure(market.VisibleCount == 1, "Category");
            market.SelectCategory("Semua");
            var listing = window.Session.Listings[0];
            window.ShowFood(listing); await Shot("food-detail");
            for (var i = 0; i <= listing.Quantity; i++) market.Add(listing);
            Ensure(window.Session.Cart.Items.Single().Quantity == listing.Quantity, "Cart stock bound");
            window.ShowCart(); await Shot("cart");
            Click(((CartView)window.CurrentPage!).CheckoutButton!);
            Ensure(window.Session.LastOrder?.TotalPrice == 96000m, "Checkout total");
            Ensure(listing.Quantity == 0 && window.Session.Cart.Items.Count == 0, "Stock reserved");
            await Shot("reservation");
            var order = window.Session.LastOrder!;
            window.ShowCustomerOrders(); await Shot("customer-orders");
            window.ShowDashboard(); await Shot("dashboard");
            window.ShowMenus(); await Shot("menus");
            window.ShowMenuEditor(); await Shot("menu-editor");
            var editor = (MenuEditorView)window.CurrentPage!;
            Click(editor.SaveButton); Ensure(window.Session.Restaurant.Menus.Count == 4, "Required menu name");
            editor.NameInput.Text = "Menu tambahan"; Click(editor.SaveButton);
            Ensure(window.Session.Restaurant.Menus.Count == 5, "Menu form saves");
            window.ShowListings(); await Shot("listings");
            window.ShowListingEditor(); await Shot("listing-editor");
            window.ShowProduction(); await Shot("production");
            window.ShowRestaurantOrders(); await Shot("restaurant-orders");
            window.ShowOrder(order, true); await Shot("restaurant-order-detail");
            Click(((OrderDetailView)window.CurrentPage!).ConfirmButton!);
            Click(((OrderDetailView)window.CurrentPage!).ReadyButton!);
            var pickup = (OrderDetailView)window.CurrentPage!;
            pickup.PickupInput!.Text = "WRONG"; Click(pickup.VerifyButton!);
            Ensure(order.Status == Replate.Domain.Enums.OrderStatus.ReadyForPickup, "Wrong pickup code");
            pickup.PickupInput.Text = order.PickupCode; Click(pickup.VerifyButton!);
            Ensure(order.Status == Replate.Domain.Enums.OrderStatus.Completed, "Cross-role pickup flow");
            await Shot("pickup-completed");
            window.Width = 1040; window.Height = 720;
            window.ShowMarketplace(); await Shot("marketplace-compact");
            window.ShowDashboard(); await Shot("dashboard-compact");
            window.ShowListingEditor(); await Shot("listing-editor-compact");
            window.ShowProduction(); await Shot("production-compact");
            Console.WriteLine($"All Replate desktop UI checks passed. Screenshots: {output}"); desktop.Shutdown();
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); desktop.Shutdown(1); }
    }
    private static async Task Settle()
    { await Task.Delay(250); await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render); }
    private static void Capture(Window window, string path)
    {
        using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height), new Vector(96, 96));
        bitmap.Render(window); bitmap.Save(path, PngBitmapEncoderOptions.Default);
    }
    private static void Ensure(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
