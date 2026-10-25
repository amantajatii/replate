using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;

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
            editor.DescriptionInput.Text = "Deskripsi tetap tersimpan di formulir";
            editor.PriceInput.Text = "";
            editor.WeightInput.Value = 0;
            Click(editor.SaveButton); await Settle();
            Ensure(window.Session.Restaurant.Menus.Count == 4, "Required menu name");
            Ensure(editor.FieldErrors["name"].Text == "Nama menu wajib diisi." && editor.FieldErrors["price"].Text == "Harga wajib diisi." &&
                editor.FieldErrors["weight"].IsVisible, "All field errors displayed together");
            Ensure(window.FocusManager?.GetFocusedElement() == editor.NameInput, "Focus first invalid field");
            Ensure(editor.DescriptionInput.Text == "Deskripsi tetap tersimpan di formulir" && editor.PriceInput.Text == "", "Invalid form retains input");
            Ensure(!window.GetVisualDescendants().OfType<TextBlock>().Any(t => t.IsVisible && t.Text == "Nama menu wajib diisi." && !editor.GetVisualDescendants().Contains(t)), "Field error not duplicated in banner");
            await Shot("menu-validation-errors");
            editor.PriceInput.Text = "abc";
            Ensure(editor.FieldErrors["price"].Text == "Harga harus berupa angka yang valid.", "Invalid number text rejected");
            editor.PriceInput.Value = 20000;
            editor.WeightInput.Value = .3m;
            editor.NameInput.Text = "Menu tambahan";
            Ensure(editor.FieldErrors.Values.All(e => !e.IsVisible), "Corrected fields clear immediately");
            Ensure(window.SuccessMessage is null, "Invalid menu has no success notification");
            editor.NameInput.Text = "Menu tambahan"; Click(editor.SaveButton);
            Ensure(window.SuccessMessage == "Menu berhasil disimpan.", "Menu creation notification");
            var savedMenu = window.Session.Restaurant.Menus.Last();
            window.ShowMenuEditor(savedMenu);
            editor = (MenuEditorView)window.CurrentPage!;
            editor.NameInput.Text = ""; editor.PriceInput.Value = 0; editor.WeightInput.Value = 0;
            Click(editor.SaveButton); await Settle();
            Ensure(savedMenu.Name == "Menu tambahan" && savedMenu.NormalPrice == 20000 && editor.FieldErrors["name"].IsVisible &&
                editor.FieldErrors["price"].IsVisible && editor.FieldErrors["weight"].IsVisible, "Edit validation prevents model changes");
            editor.NameInput.Text = "Menu diperbarui"; editor.PriceInput.Value = 20000; editor.WeightInput.Value = .3m;
            Ensure(editor.FieldErrors.Values.All(e => !e.IsVisible), "Edit corrections clear errors");
            Click(editor.SaveButton);
            Ensure(savedMenu.Name == "Menu diperbarui" && window.SuccessMessage == "Menu berhasil disimpan.", "Menu update notification");
            window.ShowDashboard();
            Ensure(window.SuccessMessage == "Menu berhasil disimpan.", "Notification survives navigation");
            await Task.Delay(4200);
            Ensure(window.SuccessMessage is null, "Success notification expires");
            window.Run(() => throw new IOException("Simulated persistence failure"), successMessage: "Menu berhasil disimpan.");
            Ensure(window.SuccessMessage is null, "Persistence failure has no success notification");
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
            var cancellationListing = window.Session.Listings[1];
            window.Session.AddToCart(cancellationListing, 1);
            var cancelledOrder = window.Session.Checkout();
            var reservedStock = cancellationListing.Quantity;
            window.ShowOrder(cancelledOrder, false); await Settle();
            Button FindButton(Control root, string text) => root.GetVisualDescendants().OfType<Button>()
                .Single(b => b.Content as string == text);
            Click(FindButton(window, "Batalkan pesanan")); await Settle();
            var dialog = window.OwnedWindows.Single();
            Ensure(dialog.Title == "Batalkan Pesanan", "Cancellation dialog title");
            Click(FindButton(dialog, "Kembali")); await Settle();
            Ensure(cancelledOrder.Status == Replate.Domain.Enums.OrderStatus.Reserved && window.SuccessMessage is null,
                "Dismissal does not cancel or notify");
            Click(FindButton(window, "Batalkan pesanan")); await Settle();
            dialog = window.OwnedWindows.Single();
            Click(FindButton(dialog, "Ya, batalkan pesanan")); await Settle();
            Ensure(cancelledOrder.Status == Replate.Domain.Enums.OrderStatus.Cancelled && cancellationListing.Quantity == reservedStock + 1,
                "Cancellation restores stock");
            Ensure(window.SuccessMessage == "Pesanan berhasil dibatalkan.", "Cancellation success notification");
            Ensure(!window.GetVisualDescendants().OfType<Button>().Any(b => b.Content as string == "Batalkan pesanan"),
                "Cancelled order view refreshed");
            await Shot("order-cancelled-notification");
            window.ShowCustomerOrders();
            Ensure(window.SuccessMessage == "Pesanan berhasil dibatalkan.", "Cancellation notification survives navigation");
            await Task.Delay(4200);
            Ensure(window.SuccessMessage is null, "Cancellation notification expires");
            window.ShowMenus(); await Settle();
            Ensure(window.GetVisualDescendants().OfType<Button>().Any(b => b.Content as string == "Hapus"), "Menu delete action exists");
            var deletion = MenuDeletionDialog.Show(window, window.Session, savedMenu); await Settle();
            dialog = window.OwnedWindows.Single();
            Ensure(dialog.Title == "Hapus Menu", "Delete dialog title");
            Ensure(dialog.FocusManager?.GetFocusedElement() == FindButton(dialog, "Kembali"), "Initial deletion focus is Back");
            Click(FindButton(dialog, "Kembali")); await deletion;
            Ensure(window.Session.Restaurant.Menus.Contains(savedMenu) && window.SuccessMessage is null, "Back keeps menu without success");
            deletion = MenuDeletionDialog.Show(window, window.Session, savedMenu); await Settle();
            Click(FindButton(window.OwnedWindows.Single(), "Hapus Menu")); await deletion; await Settle();
            Ensure(!window.Session.Restaurant.Menus.Contains(savedMenu) && window.SuccessMessage == "Menu berhasil dihapus.", "Delete succeeds and notifies");
            Ensure(!window.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == savedMenu.Name), "Deleted menu removed from list");
            await Shot("menu-deleted-notification");
            window.ShowDashboard();
            Ensure(window.SuccessMessage == "Menu berhasil dihapus.", "Delete notification survives navigation");
            await Task.Delay(4200);
            Ensure(window.SuccessMessage is null, "Delete notification expires");
            window.ShowMenus(); await Settle();
            var pricedMenu = window.Session.Listings.First(l => l.Status == Replate.Domain.Enums.ListingStatus.Active && l.Quantity > 0).Menu;
            window.ShowMenuEditor(pricedMenu); await Settle();
            var priceEditor = (MenuEditorView)window.CurrentPage!;
            var originalPrice = pricedMenu.NormalPrice;
            priceEditor.PriceInput.Value = 1000;
            Click(priceEditor.SaveButton);
            Ensure(priceEditor.FieldErrors["price"].Text?.Contains("penawaran aktif") == true && pricedMenu.NormalPrice == originalPrice && window.SuccessMessage is null,
                "Active listing price rule shown inline without saving");
            priceEditor.PriceInput.Value = originalPrice;
            Ensure(!priceEditor.FieldErrors["price"].IsVisible, "Active listing price correction clears inline error");
            window.ShowMenus(); await Settle();
            var usedMenu = window.Session.Restaurant.Menus[0];
            deletion = MenuDeletionDialog.Show(window, window.Session, usedMenu); await Settle();
            dialog = window.OwnedWindows.Single();
            Click(FindButton(dialog, "Hapus Menu")); await Settle();
            Ensure(dialog.IsVisible && window.Session.Restaurant.Menus.Contains(usedMenu) && window.SuccessMessage is null, "Used menu deletion rejected without success");
            Ensure(dialog.GetVisualDescendants().OfType<TextBlock>().Any(t => t.IsVisible && t.Text?.Contains("Gunakan Nonaktifkan") == true), "Rejection explains Nonaktifkan");
            await Shot("menu-deletion-rejected");
            dialog.Close(); await deletion;
            window.Width = 1040; window.Height = 720;
            window.ShowMarketplace(); await Shot("marketplace-compact");
            window.ShowDashboard(); await Shot("dashboard-compact");
            window.ShowListingEditor(); await Shot("listing-editor-compact");
            window.ShowProduction(); await Shot("production-compact");
            await PhotoVerification.Run(window, output);
            await ProductionVerification.Run(window, output);
            await OrderBadgeVerification.Run(output);
            await DashboardVerification.Run(output);
            await FormLayoutVerification.Run(output);
            await PickupCopyVerification.Run(output);
            await OrdersEmptyVerification.Run(output);
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
