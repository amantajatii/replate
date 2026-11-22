using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
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
            async Task Shot(string name) { await Settle(); InputVerification.Check(window, name); Capture(window, Path.Combine(output, name + ".png")); }
            void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Ensure(window.CurrentPage is SignInView && !window.Session.IsSignedIn, "Startup shows sign-in");
            var startSignIn = (SignInView)window.CurrentPage!;
            await AuthVerification.CheckSurface(window, startSignIn.Surface, "Masuk");
            Ensure(window.FocusManager?.GetFocusedElement() == startSignIn.EmailInput, "Startup focus is Email");
            AuthVerification.CheckTitle(startSignIn.TitleText, "Masuk", "Masuk");
            await AuthVerification.CheckReveal(startSignIn.PasswordInput, "Masuk");
            await PrimaryButton(startSignIn.SignInButton, false, startSignIn.Surface.FormScroll, startSignIn.Surface.FormScroll, "Masuk");
            await Shot("sign-in");
            Click(startSignIn.RegisterButton); await Settle();
            var startRegister = window.CurrentPage as RegisterView ?? throw new InvalidOperationException("Daftar akun baru opens registration");
            await AuthVerification.CheckSurface(window, startRegister.Surface, "Daftar");
            startRegister.OwnerOption.IsChecked = true;
            await AuthVerification.CheckSurface(window, startRegister.Surface, "Daftar pemilik");
            await AuthVerification.CheckTimeFields(startRegister);
            await AuthVerification.CheckRoles(startRegister);
            startRegister.OwnerOption.IsChecked = true;
            await PrimaryButton(startRegister.RegisterButton, false, startRegister.Surface.FormScroll, startRegister.Surface.FormScroll, "Daftar");
            await Shot("register-owner");
            Click(startRegister.BackButton); await Settle();
            Ensure(window.CurrentPage is SignInView, "Kembali ke masuk returns to sign-in");
            async Task SignInWithForm(string email)
            {
                var signIn = (SignInView)window.CurrentPage!;
                signIn.EmailInput.Text = email; signIn.PasswordInput.Text = DemoAccounts.Password;
                Click(signIn.SignInButton); await Settle();
            }
            await SignInWithForm(DemoAccounts.CustomerEmail);
            Ensure(window.Session.CurrentUser?.Email == DemoAccounts.CustomerEmail && window.CurrentPage is MarketplaceView, "Customer sign-in opens marketplace");
            await Shot("marketplace");
            await TopBarVerification.Run(window);
            NavbarCart(window);
            await MarketplaceVerification.Run(window);
            var market = window.Marketplace;
            Ensure(market.VisibleCount == 10, "Initial listings from all restaurants");
            await CollectionVerification.Run(window);
            market = window.Marketplace;
            Ensure(market.VisibleCount == 10 && window.TopBar.SearchBox.Text == "", "Jelajah is back with every listing and an empty search");
            await FoodDialogVerification.Run(window);
            await PrimaryButtons(window);
            await TextLinks(window);
            await CartVerification.Run(window);
            market = window.Marketplace;
            var listing = window.Session.Listings[0];
            window.ShowFood(listing); await Shot("food-detail");
            for (var i = 0; i <= listing.Quantity; i++) market.Add(listing);
            Ensure(window.Session.Cart.Items.Single().Quantity == listing.Quantity, "Cart stock bound");
            window.ShowCart(); await Shot("cart");
            var cartPage = (CartView)window.CurrentPage!;
            var orderButton = cartPage.Groups.Single().OrderButton;
            Ensure(orderButton.Content as string == CartView.OrderLabel && orderButton.IsEnabled, "Pesan is ready");
            var cartGroup = cartPage.Groups.Single();
            Ensure(cartGroup.FooterRow.Children.Contains(orderButton) && !cartGroup.Header.GetVisualDescendants().Contains(orderButton) &&
                !cartGroup.Header.GetVisualDescendants().Contains(cartGroup.TotalText) && cartGroup.TotalText.Text == DemoSession.Rupiah(96000m),
                "Pesan and the total sit in the group footer, not in the heading");
            Ensure(!cartPage.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text is { } text && (text.Contains("Ambil di restoran ini") || text.Contains("Reservasi tidak melibatkan pembayaran."))),
                "Keranjang has no pickup note and no payment note");
            orderButton.BringIntoView(); await Settle();
            await PrimaryButton(orderButton, true, cartPage.ListScroll!, cartPage.ListScroll!, "Keranjang Pesan");
            Click(orderButton); await Settle();
            var confirmation = window.Confirmation ?? throw new InvalidOperationException("Pesan opens the confirmation");
            Ensure(confirmation.TotalText.Text == DemoSession.Rupiah(96000m) && confirmation.Rows.Single().QuantityText.Text == $"{listing.Quantity} x {DemoSession.Rupiah(listing.RescuePrice)}",
                "The confirmation shows the item and the total");
            Click(confirmation.ConfirmButton); await Settle();
            Ensure(window.Session.LastOrder!.TotalPrice == 96000m, "Checkout total");
            Ensure(listing.Quantity == 0 && window.Session.Cart.Items.Count == 0, "Stock reserved");
            Ensure(window.Confirmation is null && window.CurrentPage is OrderDetailView && window.SuccessMessage == CartView.CheckoutSuccess && !window.TopBar.CartBadge.IsVisible,
                "Pesan sekarang creates the order, opens it and clears the badge when the cart becomes empty");
            await Shot("reservation");
            var order = window.Session.LastOrder!;
            window.ShowCustomerOrders(); await Shot("customer-orders");
            var splitPicks = window.Session.Restaurants.Where(r => r.Name != "Dapur Bumi")
                .Select(r => window.Session.Listings.First(l => l.Menu.RestaurantId == r.Id && l.IsAvailable(DateTime.UtcNow) && l.Menu.IsActive)).Take(2).ToList();
            foreach (var pick in splitPicks) window.Session.AddToCart(pick, 1);
            window.ShowCart(); await Settle();
            var splitCart = (CartView)window.CurrentPage!;
            Ensure(splitCart.Groups.Count == 2 && splitCart.Groups.All(g => g.OrderButton.IsEnabled), "Two restaurants show two groups, each with Pesan");
            var splitOrders = new List<Replate.Domain.Entities.Order>();
            Click(splitCart.Groups[0].OrderButton); await Settle();
            Ensure(window.Confirmation?.RestaurantId == splitPicks[0].Menu.RestaurantId && window.Confirmation.Rows.Single().Item.ListingId == splitPicks[0].Id,
                "Pesan opens the confirmation for that restaurant only");
            Click(window.Confirmation!.ConfirmButton); await Settle();
            splitOrders.Add(window.Session.LastOrder!);
            var firstName = window.Session.FindRestaurant(splitPicks[0].Menu.RestaurantId)!.Name;
            Ensure(window.CurrentPage is CartView { Groups.Count: 1 } rest && rest.Groups[0].RestaurantId == splitPicks[1].Menu.RestaurantId &&
                window.SuccessMessage == CartView.RestaurantSuccess(firstName) && window.TopBar.CartBadgeText.Text == "1" &&
                window.Session.Cart.Items.Single().ListingId == splitPicks[1].Id, "Ordering one restaurant keeps Keranjang open with the other restaurant and the notification");
            Click(((CartView)window.CurrentPage!).Groups[0].OrderButton); await Settle();
            Click(window.Confirmation!.ConfirmButton); await Settle();
            splitOrders.Add(window.Session.LastOrder!);
            Ensure(splitOrders.Select(o => o.RestaurantId).SequenceEqual(splitPicks.Select(p => p.Menu.RestaurantId)) && splitOrders.All(o => o.Items.Count == 1) &&
                window.Session.Cart.Items.Count == 0, "Each restaurant is ordered separately as its own order");
            Ensure(window.CurrentPage is OrderDetailView && window.SuccessMessage == CartView.CheckoutSuccess && !window.TopBar.CartBadge.IsVisible,
                "Ordering the last restaurant opens its order with the existing message and an empty badge");
            window.ShowCustomerOrders(); await Settle();
            var ordersPage = (OrdersView)window.CurrentPage!;
            Ensure(ordersPage.Cards.Take(2).Select(c => c.Order).SequenceEqual(splitOrders.AsEnumerable().Reverse()) &&
                ordersPage.Cards.Take(2).Select(c => c.NameText.Text).Distinct().Count() == 2 && window.SuccessMessage == CartView.CheckoutSuccess,
                "Both new orders are separate cards at the top of Pesanan saya, newest first, while the success message stays");
            await TopBarVerification.SignOut(window);
            await SignInWithForm(DemoAccounts.OwnerEmail("Dapur Bumi"));
            Ensure(window.Session.CurrentUser is Replate.Domain.Entities.RestaurantOwner && window.Session.Restaurant.Name == "Dapur Bumi" &&
                window.CurrentPage is DashboardView, "Owner sign-in opens Dapur Bumi dashboard");
            await Settle();
            Ensure(window.RailLogo is { Source: var railSource, IsEffectivelyVisible: true } rail && railSource == Ui.LogoWhite &&
                Math.Abs(rail.Bounds.Height - MainWindow.RailLogoHeight) < .5 && !window.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "replate."),
                "White logo is visible on the restaurant rail");
            TopBarVerification.CheckRatio(window.RailLogo!, "restaurant rail logo");
            await Shot("dashboard");
            window.ShowMenus(); await Shot("menus");
            await CardActions(window, "Menu restoran");
            var menusScroll = window.CurrentPage!.GetVisualDescendants().OfType<ScrollViewer>().First();
            await PrimaryButton(window.CurrentPage.GetVisualDescendants().OfType<Button>().First(b => b.Content as string == "Tambah menu"), true, menusScroll, menusScroll, "Restoran Tambah menu");
            CollectionVerification.NoLegacyCategory(window, "Menu list");
            var produceMenu = window.Session.Restaurant.Menus.First(m => m.Category == MenuCategories.Produce);
            window.ShowMenuEditor(produceMenu); await Settle();
            var categoryBox = window.CurrentPage!.GetVisualDescendants().OfType<ComboBox>().Single(c => c.ItemsSource is IEnumerable<string> items && items.Contains(MenuCategories.Produce));
            Ensure(((IEnumerable<string>)categoryBox.ItemsSource!).SequenceEqual(MenuCategories.Names) && categoryBox.SelectedItem as string == MenuCategories.Produce,
                "Menu form offers Makanan utama, Roti & kue and Buah & sayur");
            CollectionVerification.NoLegacyCategory(window, "Menu form");
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
            Ensure(window.SuccessMessage != "Menu berhasil disimpan.", "Invalid menu has no success notification");
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
            await CardActions(window, "Penawaran surplus");
            await ReopenAction(window);
            window.ShowListingEditor(); await Shot("listing-editor");
            window.ShowProduction(); await Shot("production");
            await ProductionFields(window, output);
            await DashboardPeriod(window, output);
            window.ShowRestaurantOrders(); await Shot("restaurant-orders");
            await RestaurantStatusPills(window);
            await LabelWidths(window);
            window.ShowOrder(order, true); await Shot("restaurant-order-detail");
            Click(((OrderDetailView)window.CurrentPage!).ConfirmButton!);
            Click(((OrderDetailView)window.CurrentPage!).ReadyButton!);
            var pickup = (OrderDetailView)window.CurrentPage!;
            await Settle(); Ensure(InputVerification.Check(window, "restaurant-order-pickup") > 0, "Pickup code field is checked");
            pickup.PickupInput!.Text = "WRONG"; Click(pickup.VerifyButton!);
            Ensure(order.Status == Replate.Domain.Enums.OrderStatus.ReadyForPickup, "Wrong pickup code");
            pickup.PickupInput.Text = order.PickupCode; Click(pickup.VerifyButton!);
            Ensure(order.Status == Replate.Domain.Enums.OrderStatus.Completed, "Cross-role pickup flow");
            await Shot("pickup-completed");
            var cancellationListing = window.Session.Listings[1];
            window.Session.AsCustomer();
            window.Session.AddToCart(cancellationListing, 1);
            var cancelledOrder = window.Session.Checkout(cancellationListing.Menu.RestaurantId);
            var reservedStock = cancellationListing.Quantity;
            window.ShowOrder(cancelledOrder, false); await Settle();
            await DangerButtons(window, cancelledOrder);
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
            window.Session.AsOwner();
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
            window.Session.AsCustomer();
            window.ShowMarketplace(); await Shot("marketplace-compact");
            window.Session.AsOwner();
            window.ShowDashboard(); await Shot("dashboard-compact");
            window.ShowListingEditor(); await Shot("listing-editor-compact");
            window.ShowProduction(); await Shot("production-compact");
            await PhotoVerification.Run(window, output);
            await ProductionVerification.Run(window, output);
            await OrderBadgeVerification.Run(output);
            await DashboardVerification.Run(output);
            await FormLayoutVerification.Run(output);
            await PickupCopyVerification.Run(output);
            await CustomerOrdersVerification.Run(output);
            await AuthVerification.Run(output);
            await AuthVerification.CheckSizes(window, output);
            Console.WriteLine($"All Replate desktop UI checks passed. Screenshots: {output}"); desktop.Shutdown();
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); desktop.Shutdown(1); }
    }
    private static async Task Settle()
    { await Task.Delay(250); await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render); }

    // Primary buttons
    private static readonly Color Primary = Color.Parse("#1F6B4F");
    private static readonly Color Selection = Color.Parse("#DDEDE5");
    private static readonly Color DividerColor = Color.Parse("#D7E4DF");
    private static readonly Color Danger = Color.Parse("#B3261E");
    private static readonly Color DangerTint = Color.Parse("#FBE4E1");
    private static readonly Color DangerPressed = Color.Parse("#F6D3CE");
    private sealed record Palette(string Kind, Color Rest, Color Hover, Color Pressed, Color Accent, BoxShadows Shadow);
    private static readonly Palette PrimaryPalette = new(Ui.PrimaryClass, Primary, Selection, DividerColor, Primary, Ui.PrimaryButtonHoverShadow);
    private static readonly Palette DangerPalette = new(Ui.DangerClass, Danger, DangerTint, DangerPressed, Danger, Ui.DangerButtonHoverShadow);
    private static async Task PrimaryButtons(MainWindow window)
    {
        var (width, height) = (window.Width, window.Height);
        foreach (var (w, h) in new[] { (1040d, 720d), (1920d, 1080d) })
        {
            window.Width = w; window.Height = h; window.ShowMarketplace(); await Settle();
            var size = $"{w:0}x{h:0}";
            var market = window.Marketplace;
            var add = market.Feed.AddButton(market.VisibleListings[0]);
            add.BringIntoView(); await Settle();
            await PrimaryButton(add, true, market.Scroll, market.Scroll, $"{size} Tambah");
            await DisabledPrimary(add, $"{size} Tambah");
            await HoverShape(window, add, $"{size} Tambah");
            add.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Settle();
            var dialog = window.Dialog ?? throw new InvalidOperationException($"{size}: Tambah opens the food dialog");
            await PrimaryButton(dialog.AddButton, true, dialog.Scroll, dialog.Card, $"{size} Tambah ke keranjang");
            await HoverShape(window, dialog.AddButton, $"{size} Tambah ke keranjang");
            await HoverShape(window, dialog.PlusButton, $"{size} plus");
            dialog.Change(1); await Settle();
            await HoverShape(window, dialog.MinusButton, $"{size} minus");
            await HoverShape(window, dialog.CloseButton, $"{size} close");
            Ensure(market.Scroll.Extent.Width <= market.Scroll.Viewport.Width + .5 && dialog.Scroll.Extent.Width <= dialog.Scroll.Viewport.Width + .5,
                $"{size}: no horizontal scrolling on Jelajah or in the dialog");
            dialog.Close(); await Settle();
        }
        window.Width = width; window.Height = height; window.ShowMarketplace(); await Settle();
    }
    private static async Task DangerButtons(MainWindow window, Replate.Domain.Entities.Order order)
    {
        var (width, height) = (window.Width, window.Height);
        foreach (var (w, h) in new[] { (1040d, 720d), (1920d, 1080d) })
        {
            window.Width = w; window.Height = h; window.ShowOrder(order, false); await Settle();
            var size = $"{w:0}x{h:0}";
            var view = (OrderDetailView)window.CurrentPage!;
            var cancel = view.CancelOrderButton ?? throw new InvalidOperationException($"{size}: Batalkan pesanan is shown on an active order");
            Ensure(cancel.Content as string == "Batalkan pesanan" && cancel.Classes.Contains(Ui.DangerClass) && !cancel.Classes.Contains(Ui.PrimaryClass) &&
                cancel.CornerRadius == new CornerRadius(6) && cancel.Padding == new Thickness(16, 11) && cancel.FontSize == 13,
                $"{size}: Batalkan pesanan uses the shared danger button with the compact primary shape");
            view.Scroll!.ScrollToEnd(); await Settle();
            await PrimaryButton(cancel, true, view.Scroll, view.Scroll, $"{size} Batalkan pesanan", DangerPalette);
            await DisabledPrimary(cancel, $"{size} Batalkan pesanan", DangerPalette);
            await HoverShape(window, cancel, $"{size} Batalkan pesanan");
            Ensure(view.Scroll.Extent.Width <= view.Scroll.Viewport.Width + .5, $"{size}: no horizontal scrolling on Detail pesanan");
        }
        window.Width = width; window.Height = height; window.ShowOrder(order, false); await Settle();
    }
    private static async Task PrimaryButton(Button button, bool compact, Visual scroll, Visual clipRoot, string label, Palette? palette = null)
    {
        var p = palette ?? PrimaryPalette;
        await Hold(button, ":pointerover", false);
        var presenter = Presenter(button);
        var neighbours = (button.GetVisualParent() as Panel)?.Children.Where(c => c != button).ToList() ?? [];
        Rect Place(Visual visual) => new(visual.TranslatePoint(default, scroll) ?? default, visual.Bounds.Size);
        var before = neighbours.Select(Place).ToList();
        var bounds = button.Bounds;
        var duration = Ui.PrimaryHoverDuration;
        Ensure(duration == TimeSpan.FromMilliseconds(120) && button.Classes.Contains(p.Kind) && button.Classes.Contains(Ui.CompactClass) == compact &&
            presenter.Transitions is { Count: 3 } faces && faces.All(t => t is TransitionBase { Easing: CubicEaseOut } b && b.Duration == duration) &&
            (!compact || button.Transitions is { Count: 1 } own && own[0] is TransformOperationsTransition { Easing: CubicEaseOut } scale &&
                scale.Duration == duration && button.RenderTransformOrigin == RelativePoint.Center),
            $"{label}: color, shadow{(compact ? " and scale" : "")} use one 120 ms ease-out transition");
        Face(button, p.Rest, Colors.White, $"{label} at rest");
        Ensure(presenter.BoxShadow.Count == 0 && Math.Abs(Scale(button) - 1) <= .005, $"{label}: at rest the button is flat at scale 1");

        await Hold(button, ":pointerover", true);
        Face(button, p.Hover, p.Accent, $"{label} hover");
        var shadow = presenter.BoxShadow;
        Ensure(shadow.Count == 1 && shadow[0].OffsetX == 0 && shadow[0].OffsetY == 4 && shadow[0].Blur == 12 && shadow[0].Spread == 0 &&
            shadow[0].Color.R == p.Accent.R && shadow[0].Color.G == p.Accent.G && shadow[0].Color.B == p.Accent.B && Math.Abs(shadow[0].Color.A / 255d - .30) < .01 &&
            shadow.ToString() == p.Shadow.ToString(), $"{label}: hover adds the soft {p.Kind} shadow ({shadow})");
        var hovered = Scale(button);
        Ensure(compact ? Math.Abs(hovered - Ui.PrimaryHoverScale) <= .005 && Math.Abs(Ui.PrimaryHoverScale - 1.03) <= .005 && button.ZIndex > 0 : Math.Abs(hovered - 1) <= .005,
            $"{label}: hover scale is {hovered:0.###} ({(compact ? "compact, 1.03" : "full width, no scale")})");
        Ensure(button.Bounds == bounds && neighbours.Select(Place).SequenceEqual(before),
            $"{label}: hover leaves the surrounding layout in place ({bounds} to {button.Bounds}; {string.Join(", ", before)} to {string.Join(", ", neighbours.Select(Place))})");
        Ensure(scroll is not ScrollViewer viewer || Fits(viewer), $"{label}: no horizontal scrolling while hovered");
        if (compact)
        {
            var clips = button.GetVisualAncestors().TakeWhile(a => a != clipRoot).Where(a => a.ClipToBounds).Append(clipRoot).ToList();
            Ensure(clips.All(c => button.TransformToVisual(c) is { } m && new Rect(c.Bounds.Size).Contains(new Rect(button.Bounds.Size).TransformToAABB(m))),
                $"{label}: the scaled button is not clipped by its containers");
        }
        if (button.GetVisualDescendants().OfType<Avalonia.Controls.Border>().FirstOrDefault(b => b.Name == Ui.FocusRingPart) is { } ring)
        {
            await Hold(button, ":focus-visible", true);
            Ensure(ring.BorderBrush is ISolidColorBrush { Color: var color } && color == Primary && ring.IsVisible &&
                ring.Parent is Panel frame && frame.Children.IndexOf(ring) > frame.Children.IndexOf(presenter), $"{label}: the focus ring stays visible on top while hovered");
            await Hold(button, ":focus-visible", false);
        }

        await Hold(button, ":pressed", true);
        Face(button, p.Pressed, p.Accent, $"{label} pressed");
        Ensure(TopBarVerification.Contrast(Colors.White, p.Pressed) > TopBarVerification.Contrast(Colors.White, p.Hover), $"{label}: pressed is one step darker than hover");
        await Hold(button, ":pressed", false);
        await Hold(button, ":pointerover", false);
        Face(button, p.Rest, Colors.White, $"{label} after leave");
        Ensure(presenter.BoxShadow.Count == 0 && Math.Abs(Scale(button) - 1) <= .005 && button.ZIndex == 0, $"{label}: leaving returns to flat, scale 1");
    }
    private static async Task DisabledPrimary(Button button, string label, Palette? palette = null)
    {
        var p = palette ?? PrimaryPalette;
        var presenter = Presenter(button);
        (Color, Color, int, double) State() => (Fill(presenter.Background), Fill(presenter.Foreground), presenter.BoxShadow.Count, Scale(button));
        button.IsEnabled = false; await Hold(button, ":pointerover", false);
        var rest = State();
        await Hold(button, ":pointerover", true);
        var hover = State();
        await Hold(button, ":pressed", true);
        var pressed = State();
        await Hold(button, ":pressed", false); await Hold(button, ":pointerover", false);
        button.IsEnabled = true; await Hold(button, ":pointerover", false);
        Ensure(rest == hover && rest == pressed && rest.Item1 != p.Hover && rest.Item3 == 0 && Math.Abs(rest.Item4 - 1) <= .005,
            $"{label}: disabled has no hover or press effect ({rest} vs {hover})");
    }
    private static void Face(Button button, Color background, Color foreground, string label)
    {
        var presenter = Presenter(button);
        var marks = presenter.GetVisualDescendants().Select(v => v switch { TextBlock t => t.Foreground, PathIcon i => i.Foreground, _ => null }).Where(b => b is not null).ToList();
        var contrast = TopBarVerification.Contrast(foreground, background);
        Ensure(Fill(presenter.Background) == background && Fill(presenter.Foreground) == foreground && marks.All(b => Fill(b) == foreground) &&
            Fill(presenter.Foreground) != Colors.Black && contrast >= 4.5,
            $"{label}: {Fill(presenter.Background)} fill with {Fill(presenter.Foreground)} text and icons, expected {background} and {foreground} ({contrast:0.0}:1)");
    }
    private static ContentPresenter Presenter(Button button) =>
        button.GetVisualDescendants().OfType<ContentPresenter>().First(p => p.Name == "PART_ContentPresenter");
    private static double Scale(Visual visual) => visual.RenderTransform?.Value.M11 ?? 1;
    private static Color Fill(IBrush? brush) => brush is ISolidColorBrush solid ? solid.Color : Colors.Transparent;
    private static async Task Hold(StyledElement element, string pseudo, bool on)
    { ((IPseudoClasses)element.Classes).Set(pseudo, on); await Task.Delay(Ui.PrimaryHoverDuration * 3); }
    // Hover shape
    internal static async Task HoverShape(Window window, Button button, string label)
    {
        var presenter = Presenter(button);
        var round = button.Classes.Contains(Ui.RoundClass) || button.Classes.Contains(Ui.IconClass);
        Ensure(!button.ClipToBounds && presenter.CornerRadius == button.CornerRadius, $"{label}: the hover face uses the button radius and nothing clips it to a square");
        if (round)
            Ensure(button.Width == button.Height && button.CornerRadius == new CornerRadius(button.Width / 2) && !button.Classes.Contains(Ui.CompactClass),
                $"{label}: round button is {button.Width}x{button.Height} with radius {button.CornerRadius.TopLeft}, half its size");
        await Hold(button, ":pointerover", true);
        var shadow = presenter.BoxShadow;
        var fill = Fill(presenter.Background);
        if (round) Ensure(shadow.Count == 0 && Math.Abs(Scale(button) - 1) <= .005, $"{label}: round and icon buttons have no hover shadow and no scale ({shadow}, {Scale(button):0.###})");
        if (round && button.Classes.Contains(Ui.PrimaryClass)) Face(button, Selection, Primary, $"{label} hover");
        var shaded = Snapshot(window);
        if (shadow.Count > 0) { presenter.BoxShadow = default; await Task.Delay(Ui.PrimaryHoverDuration * 3); }
        var hovered = Snapshot(window);
        if (shadow.Count > 0) { presenter.ClearValue(ContentPresenter.BoxShadowProperty); await Task.Delay(Ui.PrimaryHoverDuration * 3); }
        var opacity = button.Opacity; button.Opacity = 0;
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);
        var surface = Snapshot(window);
        button.Opacity = opacity;
        var box = new Rect(button.Bounds.Size).TransformToAABB(button.TransformToVisual(window)!.Value);
        await Hold(button, ":pointerover", false);
        var (left, top, right, bottom) = ((int)Math.Ceiling(box.X), (int)Math.Ceiling(box.Y), (int)Math.Floor(box.Right) - 1, (int)Math.Floor(box.Bottom) - 1);
        foreach (var (x, y) in new[] { (left, top), (right, top), (left, bottom), (right, bottom) })
            Ensure(Near(hovered(x, y), surface(x, y)), $"{label}: corner ({x},{y}) outside the shape keeps the surface {surface(x, y)}, not a hover tint {hovered(x, y)}");
        var (ix, iy) = ((int)(box.X + Math.Min(box.Width, box.Height) * .25), (int)(box.Y + box.Height / 2));
        Ensure(fill != Colors.Transparent && Near(hovered(ix, iy), fill), $"{label}: inside the shape the hover fill is {hovered(ix, iy)}, expected {fill}");
        if (shadow.Count == 0) return;
        var reach = new Rect(box.X + shadow[0].OffsetX - shadow[0].Blur, box.Y + shadow[0].OffsetY - shadow[0].Blur,
            box.Width + 2 * shadow[0].Blur, box.Height + 2 * shadow[0].Blur);
        var (bx, by) = ((int)box.Center.X, bottom + 4);
        Ensure(!Near(shaded(bx, by), surface(bx, by)), $"{label}: the hover shadow shows below the button ({shaded(bx, by)} on {surface(bx, by)})");
        Ensure(button.GetVisualAncestors().OfType<Visual>().Where(v => v.ClipToBounds).All(v => v.TransformToVisual(window) is { } m && new Rect(v.Bounds.Size).TransformToAABB(m).Contains(reach)),
            $"{label}: no container clips the hover shadow ({reach})");
    }
    // Card actions
    private static async Task CardActions(MainWindow window, string page)
    {
        var buttons = window.CurrentPage!.GetVisualDescendants().OfType<Button>().Where(b => b.Classes.Contains(Ui.CardActionClass)).ToList();
        Ensure(buttons.Count > 0, $"{page}: card action buttons exist");
        foreach (var button in buttons)
        {
            var label = $"{page} {button.Content}";
            var (hover, pressed, text) =
                button.Classes.Contains(Ui.PrimaryClass) ? (Selection, DividerColor, Primary) :
                button.Classes.Contains(Ui.CardActionDangerClass) ? (DangerTint, DangerPressed, Danger) :
                button.Classes.Contains(Ui.CardActionTonalClass) ? (Primary, Primary, Colors.White) :
                button.Classes.Contains(Ui.CardActionNeutralClass) ? (Selection, DividerColor, Primary) :
                throw new InvalidOperationException($"{label}: card action has a role class");
            var presenter = Presenter(button);
            button.BringIntoView(); await Settle();
            var (restFill, restText, restBorder) = (Fill(presenter.Background), Fill(presenter.Foreground), Fill(presenter.BorderBrush));
            var (bounds, thickness, radius) = (button.Bounds, presenter.BorderThickness, presenter.CornerRadius);
            Ensure(!button.ClipToBounds && radius == button.CornerRadius && button.Classes.Contains(Ui.CompactClass) &&
                presenter.BoxShadow.Count == 0 && Math.Abs(Scale(button) - 1) <= .005,
                $"{label}: at rest the face uses the button radius, is not clipped, and is flat at scale 1");
            foreach (var (state, fill) in new[] { (":pointerover", hover), (":pressed", pressed) })
            {
                await Hold(button, state, true);
                var (shownFill, ink, edge) = (Fill(presenter.Background), Fill(presenter.Foreground), Fill(presenter.BorderBrush));
                var contrast = TopBarVerification.Contrast(ink, shownFill);
                Ensure(shownFill == fill && shownFill != restFill && ink == text && contrast >= 4.5,
                    $"{label} {state}: {shownFill} fill with {ink} text, expected {fill} and {text}, unlike rest {restFill} ({contrast:0.0}:1)");
                Ensure(edge == restBorder && presenter.BorderThickness == thickness && presenter.CornerRadius == radius,
                    $"{label} {state}: border stays {restBorder} at {thickness} ({edge}, {presenter.BorderThickness})");
                Ensure(button.Bounds == bounds, $"{label} {state}: layout bounds stay {bounds} ({button.Bounds})");
                if (state == ":pointerover")
                    Ensure(Math.Abs(Scale(button) - Ui.PrimaryHoverScale) <= .005 && presenter.BoxShadow.Count > 0,
                        $"{label} hover: the shared compact scale and soft shadow ({Scale(button):0.###})");
                await Hold(button, state, false);
            }
            Ensure(Fill(presenter.Background) == restFill && Fill(presenter.Foreground) == restText && Fill(presenter.BorderBrush) == restBorder &&
                button.Bounds == bounds, $"{label}: returns to its normal colors and size");
            await HoverShape(window, button, label);
        }
    }
    // Reopen action
    private static async Task ReopenAction(MainWindow window)
    {
        var now = DateTime.UtcNow;
        var listing = window.Session.RestaurantListings.FirstOrDefault(l => l.IsAvailable(now) && l.Menu.IsActive)
            ?? throw new InvalidOperationException("Penawaran surplus: an open listing exists to close for Buka kembali");
        listing.Cancel();
        try
        {
            Ensure(DemoSession.ReopenBlocker(listing, now) is null, "Penawaran surplus: the closed listing can be reopened");
            window.ShowListings(); await Settle();
            var page = window.CurrentPage!;
            var cardButtons = page.GetVisualDescendants().OfType<Button>().Where(b => b.Classes.Contains(Ui.CardActionClass)).ToList();
            var reopen = cardButtons.Single(b => b.Content as string == "Buka kembali");
            var edit = cardButtons.FirstOrDefault(b => b.Content as string == "Ubah");
            const string label = "Penawaran surplus Buka kembali";
            Ensure(reopen.Classes.Contains(Ui.PrimaryClass) && reopen.Classes.Contains(Ui.CompactClass) &&
                !reopen.Classes.Contains(Ui.CardActionTonalClass) && !reopen.Classes.Contains(Ui.CardActionNeutralClass),
                $"{label}: uses the shared primary button");
            Ensure(edit is null || (reopen.Bounds.Height == edit.Bounds.Height && reopen.CornerRadius == edit.CornerRadius &&
                reopen.Padding == edit.Padding && reopen.FontSize == edit.FontSize),
                $"{label}: same compact height and radius as Ubah ({reopen.Bounds.Height} vs {edit?.Bounds.Height})");
            reopen.BringIntoView(); await Settle();
            var presenter = Presenter(reopen);
            Ensure(Fill(presenter.Background) == Primary, $"{label}: rest fill is the primary token ({Fill(presenter.Background)})");
            var restFill = Fill(presenter.Background);
            await Hold(reopen, ":pointerover", true);
            Ensure(Fill(presenter.Background) != restFill, $"{label}: hover changes the fill ({Fill(presenter.Background)})");
            await Hold(reopen, ":pointerover", false);
            var scroll = reopen.GetVisualAncestors().OfType<ScrollViewer>().First();
            await PrimaryButton(reopen, true, scroll, scroll, label);
            await DisabledPrimary(reopen, label);
        }
        finally
        {
            listing.Reopen();
            window.ShowListings(); await Settle();
        }
    }
    // Restaurant status pills
    private static async Task RestaurantStatusPills(MainWindow window)
    {
        var (width, height) = (window.Width, window.Height);
        void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        int Expected(int index) => window.Session.RestaurantOrders.Count(o => index switch
        {
            0 => o.Status is not (Replate.Domain.Enums.OrderStatus.Completed or Replate.Domain.Enums.OrderStatus.Cancelled),
            2 => o.Status == Replate.Domain.Enums.OrderStatus.Completed, 3 => o.Status == Replate.Domain.Enums.OrderStatus.Cancelled, _ => true
        });
        foreach (var (w, h) in new[] { (1040d, 720d), (1920d, 1080d) })
        {
            window.Width = w; window.Height = h;
            var size = $"{w:0}x{h:0}";
            window.ShowRestaurantOrders(); await Settle();
            var page = (OrdersView)window.CurrentPage!;
            var pills = page.StatusButtons;
            Ensure(!page.GetVisualDescendants().OfType<ComboBox>().Any() && pills.Count == OrdersView.StatusOptions.Length &&
                pills.Select(p => p.Content as string).SequenceEqual(OrdersView.StatusOptions) &&
                pills.All(p => Avalonia.Automation.AutomationProperties.GetName(p) == p.Content as string && p.Classes.Contains(OrdersView.StatusClass)),
                $"{size}: one named pill per status option, in the dropdown order");
            Ensure(page.StatusIndex == 0 && pills[0].Classes.Contains(Ui.PillSelectedClass) && pills.Skip(1).All(p => !p.Classes.Contains(Ui.PillSelectedClass)),
                $"{size}: Aktif is the initial pill");
            Ensure(pills.All(p => p.Bounds.Height >= Ui.PillHeight - .5 && Math.Abs(p.CornerRadius.TopLeft - p.Bounds.Height / 2) < .5 && p.Focusable && p.IsTabStop),
                $"{size}: pills are at least 36 high, fully rounded and reachable with Tab");
            void Faces(int selected)
            {
                for (var i = 0; i < pills.Count; i++)
                {
                    var presenter = Presenter(pills[i]);
                    var expected = i == selected ? (Primary, Colors.White) : (Colors.White, Color.Parse("#202522"));
                    Ensure(Fill(presenter.Background) == expected.Item1 && Fill(presenter.Foreground) == expected.Item2 &&
                        (i == selected || Fill(presenter.BorderBrush) == DividerColor) && Math.Abs(Scale(pills[i]) - 1) <= .005,
                        $"{size}: pill {pills[i].Content} shows {Fill(presenter.Background)} with {Fill(presenter.Foreground)} text, expected {expected.Item1} and {expected.Item2}");
                }
            }
            Faces(0);
            var gap = pills[1].Bounds.X - pills[0].Bounds.Right;
            Ensure(Math.Abs(gap - Ui.PillGap) < .5 || pills[1].Bounds.Y > pills[0].Bounds.Y, $"{size}: pills sit {gap:0.#} apart");
            var label = page.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == OrdersView.StatusLabel);
            var labelBottom = label.TranslatePoint(new Point(0, label.Bounds.Height), page)!.Value.Y;
            var rowTop = page.StatusRow!.TranslatePoint(default, page)!.Value.Y;
            Ensure(Math.Abs(rowTop - labelBottom - 8) < .5, $"{size}: label sits {rowTop - labelBottom:0.#} above the pills");
            var scroll = page.GetVisualDescendants().OfType<ScrollViewer>().First();
            for (var i = 0; i < pills.Count; i++)
            {
                Click(pills[i]); await Settle();
                page = (OrdersView)window.CurrentPage!; pills = page.StatusButtons;
                var badges = page.GetVisualDescendants().OfType<OrderStatusBadge>().Count();
                Ensure(page.StatusIndex == i && badges == Expected(i), $"{size}: {pills[i].Content} lists {badges} orders, expected {Expected(i)}");
                Faces(i);
                Ensure(pills.Count(p => p.Classes.Contains(Ui.PillSelectedClass)) == 1, $"{size}: only {pills[i].Content} is selected");
            }
            Ensure(Fits(scroll) && page.StatusRow!.Bounds.Right <= scroll.Viewport.Width + .5, $"{size}: no horizontal scrolling on Pesanan restoran");
            Click(pills[0]); await Settle();
        }
        window.Width = width; window.Height = height; window.ShowRestaurantOrders(); await Settle();
    }

    // Production fields
    private static readonly Color Primary500 = Color.Parse("#59AC87");
    private static readonly Color MutedColor = Color.Parse("#69736D");
    private static async Task ProductionFields(MainWindow window, string output)
    {
        var (width, height) = (window.Width, window.Height);
        void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        foreach (var (w, h) in new[] { (1040d, 720d), (1920d, 1080d) })
        {
            window.Width = w; window.Height = h;
            var size = $"{w:0}x{h:0}";
            window.ShowProduction(); await Settle();
            var view = (ProductionView)window.CurrentPage!;
            var frames = view.Frames;
            Ensure(frames.Keys.SequenceEqual(new[] { "menu", "date", "produced", "sold" }), $"{size}: Menu, Tanggal produksi, Diproduksi and Terjual reguler use the shared field");
            Ensure(!view.GetVisualDescendants().OfType<DatePicker>().Any() && !view.ProducedInput.ShowButtonSpinner && !view.SoldInput.ShowButtonSpinner,
                $"{size}: no three-part date picker and no native spinner");

            Avalonia.Input.InputElement Target(string key) => key switch
            {
                "menu" => view.MenuInput,
                "date" => view.DateInput.Face,
                _ => frames[key].Input.GetVisualDescendants().OfType<TextBox>().First()
            };
            foreach (var (key, frame) in frames)
            {
                var face = frame.Face;
                Ensure(Fill(face.Background) == Colors.White && Fill(face.BorderBrush) == DividerColor && face.BorderThickness == new Thickness(1) &&
                    face.CornerRadius == new CornerRadius(Ui.FieldRadius) && Math.Abs(face.Bounds.Height - Ui.FieldHeight) < .5,
                    $"{size} {key}: white field, thin divider border, radius 12, 48 high ({Fill(face.BorderBrush)}, {face.CornerRadius}, {face.Bounds.Height:0.#})");
                await Hold(face, ":pointerover", true);
                Ensure(Fill(face.BorderBrush) == Primary500, $"{size} {key}: hover turns the border Primary 500 ({Fill(face.BorderBrush)})");
                await Hold(face, ":pointerover", false);
                Ensure(Fill(face.BorderBrush) == DividerColor, $"{size} {key}: leaving restores the divider border");

                window.Focus(); await Settle();
                Target(key).Focus(Avalonia.Input.NavigationMethod.Tab); await Settle();
                var ring = frame.Ring;
                Ensure(frame.KeyboardFocus && ring.IsVisible && Fill(ring.BorderBrush) == Primary && ring.BorderThickness == new Thickness(Ui.FocusRingWidth) &&
                    ring.Margin == new Thickness(-(Ui.FocusRingWidth + Ui.FocusRingOffset)) && Fill(face.BorderBrush) == Primary,
                    $"{size} {key}: keyboard focus shows the Primary 700 border and the shared 2 px ring 2 outside");
                Ensure(Target(key) is Control focused && (focused.FocusAdorner is null || focused.FocusAdorner == Ui.NoFocusAdorner),
                    $"{size} {key}: no native black focus box");
                window.Focus(); await Settle();
                Ensure(!ring.IsVisible && Fill(face.BorderBrush) == DividerColor, $"{size} {key}: leaving the field hides the ring");
                Target(key).Focus(Avalonia.Input.NavigationMethod.Pointer); await Settle();
                Ensure(!ring.IsVisible && Fill(face.BorderBrush) == Primary, $"{size} {key}: a click focus shows the Primary 700 border without the ring");
                window.Focus(); await Settle();
            }

            var chevron = view.MenuInput.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>().FirstOrDefault();
            Ensure(chevron is { Fill: null } && chevron.StrokeThickness > 0 && chevron.StrokeThickness <= 2 && Fill(chevron.Stroke) == MutedColor,
                $"{size}: the Menu dropdown shows a thin outline chevron");
            view.MenuInput.IsDropDownOpen = true; await Settle();
            var popup = view.MenuInput.GetVisualDescendants().OfType<Popup>().First();
            Ensure(popup.IsOpen && popup.Child is Avalonia.Controls.Border { BorderThickness.Left: 1 } sheet && Fill(sheet.BorderBrush) == DividerColor &&
                sheet.CornerRadius == new CornerRadius(Ui.FieldRadius) && frames["menu"].Face.Classes.Contains(Ui.OpenClass) && Fill(frames["menu"].Face.BorderBrush) == Primary,
                $"{size}: the Menu list opens as a rounded sheet with a thin border");
            var selected = view.MenuInput.ContainerFromIndex(view.MenuInput.SelectedIndex) as ComboBoxItem;
            Ensure(selected is not null && selected.FontWeight == FontWeight.SemiBold && (selected.FocusAdorner is null || selected.FocusAdorner == Ui.NoFocusAdorner),
                $"{size}: the selected menu is semibold without a black focus box");
            var other = Enumerable.Range(0, view.MenuInput.ItemCount).Where(i => i != view.MenuInput.SelectedIndex)
                .Select(i => view.MenuInput.ContainerFromIndex(i) as ComboBoxItem).FirstOrDefault(i => i is not null);
            if (other is not null)
            {
                Ensure(other.FontWeight == FontWeight.Normal && Fill(other.Background) == Colors.Transparent, $"{size}: other menus rest normal and transparent");
                await Hold(other, ":pointerover", true);
                Ensure(Fill(other.Background) == Selection, $"{size}: a hovered menu uses the Primary 100 fill ({Fill(other.Background)})");
                await Hold(other, ":pointerover", false);
            }
            view.MenuInput.IsDropDownOpen = false; await Settle();
            Ensure(!frames["menu"].Face.Classes.Contains(Ui.OpenClass), $"{size}: closing the Menu list clears the open border");

            var date = view.DateInput;
            var today = DateTime.Today;
            Ensure(date.ValueText.Text == today.ToString(Ui.DateFormat, Ui.Indonesian) && date.ValueText.Text?.Contains('—') == false &&
                date.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>().Any(p => p.Fill is null && Fill(p.Stroke) == MutedColor),
                $"{size}: the date reads \"{date.ValueText.Text}\" in Indonesian with a calendar icon on the right");
            date.Open(false); await Settle();
            Ensure(date.Popup.IsOpen && date.Calendar.SelectedDate == today && date.Face.Classes.Contains(Ui.OpenClass) && Fill(date.Face.BorderBrush) == Primary,
                $"{size}: the calendar opens on the selected date");
            var day = date.Calendar.GetVisualDescendants().OfType<CalendarDayButton>().FirstOrDefault(b => b.Classes.Contains(":selected"));
            Ensure(day is not null && day.Theme == Ui.DayTheme && Fill(day.Background) == Primary && Fill(day.Foreground) == Colors.White,
                $"{size}: the selected day uses the Primary 700 fill with white text");
            await CalendarLook(date, size);
            var pick = today.AddDays(-1);
            date.Calendar.SelectedDate = pick; await Settle();
            Ensure(date.SelectedDate?.Date == pick && date.ValueText.Text == pick.ToString(Ui.DateFormat, Ui.Indonesian),
                $"{size}: picking a day updates the field ({date.ValueText.Text})");
            date.Close(false); await Settle();
            Ensure(!date.Popup.IsOpen && !date.Face.Classes.Contains(Ui.OpenClass), $"{size}: the calendar closes");
            date.SelectedDate = DateTimeOffset.Now; await Settle();

            foreach (var stepper in new[] { view.ProducedStepper, view.SoldStepper })
            {
                var input = stepper.Input;
                var original = input.Value;
                Ensure(new[] { stepper.MinusButton, stepper.PlusButton }.All(b => b.Classes.Contains(Ui.RoundClass) && Math.Abs(b.Bounds.Width - QuantityStepper.ButtonSize) < .5 &&
                        b.Bounds.Width == b.Bounds.Height) && stepper.MinusButton.Bounds.Right <= stepper.Frame.Bounds.Left && stepper.PlusButton.Bounds.Left >= stepper.Frame.Bounds.Right,
                    $"{size}: round minus and plus buttons sit left and right of the number");
                input.Value = input.Minimum; await Settle();
                Ensure(!stepper.MinusButton.IsEnabled && stepper.PlusButton.IsEnabled, $"{size}: minus is disabled at the minimum");
                Click(stepper.PlusButton); await Settle();
                Ensure(input.Value == input.Minimum + input.Increment && input.Text == (input.Minimum + input.Increment).ToString("0") && stepper.MinusButton.IsEnabled,
                    $"{size}: plus raises the number ({input.Text})");
                Click(stepper.MinusButton); await Settle();
                Ensure(input.Value == input.Minimum, $"{size}: minus lowers the number");
                stepper.Step(-1); await Settle();
                Ensure(input.Value == input.Minimum, $"{size}: the minimum holds");
                input.Value = input.Maximum; await Settle();
                Ensure(!stepper.PlusButton.IsEnabled && stepper.MinusButton.IsEnabled, $"{size}: plus is disabled at the maximum");
                stepper.Step(1); await Settle();
                Ensure(input.Value == input.Maximum, $"{size}: the maximum holds");
                input.Value = original; await Settle();
            }
            var produced = view.ProducedInput.Value ?? 0;
            var sold = view.SoldInput.Value;
            view.SoldInput.Value = produced; await Settle();
            Click(view.SoldStepper.PlusButton); await Settle();
            Click(view.SaveButton); await Settle();
            Ensure(view.FieldErrors["sold"].IsVisible && frames["sold"].Invalid && Fill(frames["sold"].Face.BorderBrush) == Danger,
                $"{size}: selling more than produced is still reported inline with the danger border");
            view.SoldInput.Value = sold; await Settle();
            Ensure(!frames["sold"].Invalid, $"{size}: correcting the number clears the danger border");

            var grid = view.Fields;
            var cells = grid.Children.OfType<Control>().ToList();
            Rect At(Control c) => new(c.TranslatePoint(default, grid)!.Value, c.Bounds.Size);
            Ensure(grid.Columns == 2 && cells.Count == 4 && Math.Abs(At(cells[0]).Width - At(cells[1]).Width) < .5 &&
                Math.Abs(At(cells[2]).X - At(cells[0]).X) < .5 && Math.Abs(At(cells[2]).Width - At(cells[0]).Width) < .5 &&
                Math.Abs(At(cells[3]).X - At(cells[1]).X) < .5 && Math.Abs(At(cells[3]).Width - At(cells[1]).Width) < .5,
                $"{size}: two equal columns, and the second row is as wide as the first");
            Ensure(Math.Abs(At(cells[1]).X - At(cells[0]).Right - Ui.Spacing.Related) < .5 && Math.Abs(At(cells[2]).Y - At(cells[0]).Bottom - Ui.Spacing.Related) < .5,
                $"{size}: 16 between columns and rows");
            foreach (var (key, frame) in frames)
            {
                var stack = (StackPanel)cells[frames.Keys.ToList().IndexOf(key)];
                var label = (TextBlock)stack.Children[0];
                var gap = stack.Children[1].Bounds.Top - label.Bounds.Bottom;
                Ensure(label.FontSize == 14 && label.FontWeight == FontWeight.SemiBold && Math.Abs(gap - Ui.FieldGap) < .5, $"{size} {key}: semibold 14 label 8 above the field ({gap:0.#})");
            }
            grid.Fit(FieldGrid.NarrowWidth); await Settle();
            Ensure(grid.Columns == 1 && cells.All(c => Math.Abs(At(c).X) < .5), $"{size}: 900 or less stacks the fields in one column");
            grid.Fit(window.ClientSize.Width); await Settle();
            Ensure(grid.Columns == 2, $"{size}: wide windows use two columns");

            var card = view.FormCard;
            Ensure(Fill(card.Background) == Colors.White && Fill(card.BorderBrush) == DividerColor && card.BorderThickness == new Thickness(1) &&
                card.CornerRadius == new CornerRadius(Ui.FormCardRadius) && card.Padding == new Thickness(Ui.Spacing.Section),
                $"{size}: the form is one white card with a thin border, radius 16 and padding 24");
            var note = view.RescuedNote;
            var count = view.RescuedCount;
            var noteTop = note.TranslatePoint(default, card)!.Value.Y;
            var gridBottom = grid.TranslatePoint(new Point(0, grid.Bounds.Height), card)!.Value.Y;
            Ensure(count.FontSize == 28 && count.FontWeight == FontWeight.Bold && Fill(count.Foreground) == Primary && note.Text == ProductionView.RescuedNoteText &&
                Fill(note.Foreground) == MutedColor && noteTop > gridBottom && note.Text?.Contains('—') == false,
                $"{size}: the rescued summary sits under the fields with a large Primary 700 number and a muted note");
            var save = view.SaveButton;
            var corner = save.TranslatePoint(new Point(save.Bounds.Width, save.Bounds.Height), card)!.Value;
            var inner = card.Padding.Right + card.BorderThickness.Right;
            Ensure(save.Classes.Contains(Ui.PrimaryClass) && save.Classes.Contains(Ui.CompactClass) &&
                Math.Abs(corner.X - (card.Bounds.Width - inner)) < .5 && Math.Abs(corner.Y - (card.Bounds.Height - inner)) < .5,
                $"{size}: Simpan catatan is the shared primary button in the bottom right of the card ({corner})");
            var form = card.FindAncestorOfType<FormArea>()!;
            var heading = view.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == "Catatan produksi");
            var headingBlock = heading.FindAncestorOfType<Grid>()!;
            var rhythm = form.TranslatePoint(default, view)!.Value.Y - headingBlock.TranslatePoint(new Point(0, headingBlock.Bounds.Height), view)!.Value.Y;
            Ensure(Math.Abs(rhythm - Ui.Spacing.Section) < .5, $"{size}: 24 between the heading and the form card ({rhythm:0.#})");

            var scroll = view.GetVisualDescendants().OfType<ScrollViewer>().First();
            Ensure(Fits(scroll), $"{size}: no horizontal scrolling on Catatan produksi");
            foreach (var text in card.GetVisualDescendants().OfType<TextBlock>().Where(t => t.IsEffectivelyVisible && !string.IsNullOrEmpty(t.Text)))
            {
                var x = text.TranslatePoint(default, card)!.Value.X;
                Ensure(text.TextLayout.TextLines.All(l => !l.HasCollapsed) && x >= -.5 && x + text.Bounds.Width <= card.Bounds.Width + .5,
                    $"{size}: \"{text.Text}\" is shown in full inside the card");
            }
            await LabelFits(window, view, $"{size} Catatan produksi");
            Capture(window, Path.Combine(output, $"production-fields-{size}.png"));
        }
        window.Width = width; window.Height = height; window.ShowProduction(); await Settle();
    }

    // Dashboard period
    private static async Task DashboardPeriod(MainWindow window, string output)
    {
        var (width, height) = (window.Width, window.Height);
        var ink = Color.Parse("#202522");
        foreach (var (w, h) in new[] { (1040d, 720d), (1920d, 1080d) })
        {
            window.Width = w; window.Height = h;
            var size = $"{w:0}x{h:0}";
            window.ShowDashboard(); await Settle();
            var view = (DashboardView)window.CurrentPage!;
            var combo = view.PeriodPicker;
            var frame = view.PeriodFrame;
            var face = frame.Face;
            Ensure(frame.Input == combo && combo.Theme == Ui.SelectTheme && combo.ItemContainerTheme == Ui.OptionTheme && combo.Padding == Ui.FieldPadding &&
                Fill(combo.Foreground) == ink, $"{size}: the period dropdown uses the shared select field with Text Primary text");
            Ensure(Fill(face.Background) == Colors.White && Fill(face.BorderBrush) == DividerColor && Fill(face.BorderBrush) != Colors.Black &&
                face.BorderThickness == new Thickness(1) && face.CornerRadius == new CornerRadius(Ui.FieldRadius) && Math.Abs(face.Bounds.Height - Ui.CompactFieldHeight) < .5,
                $"{size}: the period dropdown is white with a thin divider border, radius 12, 44 high ({Fill(face.BorderBrush)}, {face.Bounds.Height:0.#})");
            var chevron = combo.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>().FirstOrDefault();
            Ensure(chevron is { Fill: null } && chevron.StrokeThickness > 0 && chevron.StrokeThickness <= 2 && Fill(chevron.Stroke) == MutedColor,
                $"{size}: the period dropdown shows a thin outline chevron on the right");

            await Hold(face, ":pointerover", true);
            Ensure(Fill(face.BorderBrush) == Primary500, $"{size}: period hover turns the border Primary 500 ({Fill(face.BorderBrush)})");
            await Hold(face, ":pointerover", false);
            Ensure(Fill(face.BorderBrush) == DividerColor, $"{size}: leaving the period dropdown restores the divider border");

            window.Focus(); await Settle();
            combo.Focus(Avalonia.Input.NavigationMethod.Tab); await Settle();
            var ring = frame.Ring;
            Ensure(frame.KeyboardFocus && ring.IsVisible && Fill(ring.BorderBrush) == Primary && ring.BorderThickness == new Thickness(Ui.FocusRingWidth) &&
                Fill(face.BorderBrush) == Primary, $"{size}: keyboard focus on the period shows the Primary 700 border and the shared ring");
            Ensure(combo.FocusAdorner is null || combo.FocusAdorner == Ui.NoFocusAdorner, $"{size}: the period dropdown has no native black focus box");
            window.Focus(); await Settle();
            Ensure(!ring.IsVisible && Fill(face.BorderBrush) == DividerColor, $"{size}: leaving the period dropdown hides the ring");
            combo.Focus(Avalonia.Input.NavigationMethod.Pointer); await Settle();
            Ensure(!ring.IsVisible && Fill(face.BorderBrush) == Primary, $"{size}: a click focus on the period shows the Primary 700 border without the ring");
            window.Focus(); await Settle();

            combo.IsDropDownOpen = true; await Settle();
            var popup = combo.GetVisualDescendants().OfType<Popup>().First();
            Ensure(popup.IsOpen && popup.Child is Avalonia.Controls.Border { BorderThickness.Left: 1 } sheet && Fill(sheet.BorderBrush) == DividerColor &&
                sheet.CornerRadius == new CornerRadius(Ui.FieldRadius) && Fill(face.BorderBrush) == Primary,
                $"{size}: the period list opens as a rounded sheet with a thin border");
            var items = Enumerable.Range(0, combo.ItemCount).Select(i => combo.ContainerFromIndex(i) as ComboBoxItem).ToList();
            Ensure(items.All(i => i is not null && (i.FocusAdorner is null || i.FocusAdorner == Ui.NoFocusAdorner) &&
                    i.GetVisualDescendants().OfType<ContentPresenter>().All(p => p.BorderThickness == default)),
                $"{size}: period options have no outline and no native focus box");
            Ensure(items[combo.SelectedIndex]!.FontWeight == FontWeight.SemiBold, $"{size}: the selected period is semibold");
            var other = items.Where((_, i) => i != combo.SelectedIndex).First()!;
            Ensure(other.FontWeight == FontWeight.Normal && Fill(other.Background) == Colors.Transparent, $"{size}: other periods rest normal and transparent");
            foreach (var pseudo in new[] { ":pointerover", ":focus-visible" })
            {
                await Hold(other, pseudo, true);
                Ensure(Fill(other.Background) == Selection && Fill(other.Foreground) == Primary &&
                    other.GetVisualDescendants().OfType<ContentPresenter>().All(p => p.BorderThickness == default),
                    $"{size}: {pseudo} on a period uses the Primary 100 fill with Primary 700 text and no outline");
                await Hold(other, pseudo, false);
            }
            combo.IsDropDownOpen = false; await Settle();
            Ensure(!face.Classes.Contains(Ui.OpenClass), $"{size}: closing the period list clears the open border");

            for (var index = 0; index < DashboardView.PeriodOptions.Length; index++)
            {
                ((DashboardView)window.CurrentPage!).PeriodPicker.SelectedIndex = index; await Settle();
                view = (DashboardView)window.CurrentPage!;
                var option = DashboardView.PeriodOptions[index];
                var shown = view.PeriodPicker.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault(t => t.IsEffectivelyVisible && t.Text == option);
                var arrow = view.PeriodPicker.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>().First();
                Ensure(shown is not null && shown.TextLayout.TextLines.All(l => !l.HasCollapsed) &&
                    shown.TranslatePoint(new Point(shown.Bounds.Width, 0), arrow)!.Value.X <= .5 && view.PeriodFrame.Bounds.Width >= Ui.CompactSelectMinWidth - .5,
                    $"{size}: \"{option}\" is shown in full before the chevron");
            }
            view.PeriodPicker.SelectedIndex = 1; await Settle();
            view = (DashboardView)window.CurrentPage!;
            var message = view.Banner.Message;
            var gap = view.PeriodFrame.TranslatePoint(default, message)!.Value.Y - message.Bounds.Height;
            Ensure(Math.Abs(gap - 8) < .5, $"{size}: the period dropdown sits 8 under the subtitle ({gap:0.#})");

            var profile = view.ProfileButton;
            var presenter = Presenter(profile);
            Ensure(profile.Classes.Contains(DashboardView.ActionClass) && Fill(presenter.Background) == Colors.White && Fill(presenter.BorderBrush) == DividerColor &&
                (profile.FocusAdorner is null || profile.FocusAdorner == Ui.NoFocusAdorner), $"{size}: Ubah profil uses the shared solid pill without a native border or focus box");
            await Hold(profile, ":pointerover", true);
            Ensure(Fill(presenter.Background) == Selection && Fill(presenter.Foreground) == Primary, $"{size}: Ubah profil hover uses the Primary 100 fill");
            await Hold(profile, ":pointerover", false);

            var scroll = view.GetVisualDescendants().OfType<ScrollViewer>().First();
            Ensure(Fits(scroll), $"{size}: no horizontal scrolling on the dashboard");
            Capture(window, Path.Combine(output, $"dashboard-period-{size}.png"));
        }
        window.Width = width; window.Height = height; window.ShowDashboard(); await Settle();
    }

    // Button label widths
    private static async Task LabelWidths(MainWindow window)
    {
        var (width, height) = (window.Width, window.Height);
        foreach (var (w, h) in new[] { (1040d, 720d), (1920d, 1080d) })
        {
            window.Width = w; window.Height = h;
            var size = $"{w:0}x{h:0}";
            window.ShowRestaurantOrders(); await Settle();
            var orders = window.CurrentPage!.GetVisualDescendants().OfType<Button>().Where(b => b.Content as string == "Lihat pesanan").ToList();
            Ensure(orders.Count == window.Session.RestaurantOrders.Count(o => o.Status is not (Replate.Domain.Enums.OrderStatus.Completed or Replate.Domain.Enums.OrderStatus.Cancelled)),
                $"{size}: one Lihat pesanan button per active order ({orders.Count})");
            await LabelFits(window, window.CurrentPage!, $"{size} Pesanan restoran");
            window.ShowMenus(); await Settle(); await LabelFits(window, window.CurrentPage!, $"{size} Menu restoran");
            window.ShowListings(); await Settle(); await LabelFits(window, window.CurrentPage!, $"{size} Penawaran");
            window.Session.AsCustomer();
            window.ShowMarketplace(); await Settle(); await LabelFits(window, window.Marketplace, $"{size} Jelajah");
            window.Session.AsOwner();
        }
        window.Width = width; window.Height = height; window.ShowRestaurantOrders(); await Settle();
    }
    private static async Task LabelFits(Window window, Control page, string label)
    {
        var buttons = page.GetVisualDescendants().OfType<Button>()
            .Where(b => b.IsEffectivelyVisible && b.Content is string { Length: > 0 } && b.Padding.Left > 0 && b.Padding.Right > 0).ToList();
        Ensure(buttons.Count > 0, $"{label}: has labelled buttons to measure");
        foreach (var button in buttons)
        {
            button.BringIntoView(); await Settle();
            var text = (string)button.Content!;
            var probe = new TextBlock { Text = text, FontSize = button.FontSize, FontWeight = button.FontWeight, FontFamily = button.FontFamily, FontStyle = button.FontStyle };
            probe.Measure(Size.Infinity);
            var need = probe.DesiredSize.Width + button.Padding.Left + button.Padding.Right + button.BorderThickness.Left + button.BorderThickness.Right;
            Ensure(button.Bounds.Width >= need - .5, $"{label} {text}: button is {button.Bounds.Width:0.#} wide, text and padding need {need:0.#}");
            var shown = Presenter(button).GetVisualDescendants().OfType<TextBlock>().FirstOrDefault(t => t.Text == text);
            Ensure(shown is null || (shown.Bounds.Width >= probe.DesiredSize.Width - .5 && shown.Bounds.Height <= probe.DesiredSize.Height + .5),
                $"{label} {text}: label shows {shown?.Bounds.Width:0.#} of {probe.DesiredSize.Width:0.#} on one line");
            var compact = button.Classes.Contains(Ui.CompactClass) && button.IsEffectivelyEnabled;
            if (compact) await Hold(button, ":pointerover", true);
            var box = new Rect(button.Bounds.Size).TransformToAABB(button.TransformToVisual(window)!.Value);
            var scale = Scale(button);
            if (compact) await Hold(button, ":pointerover", false);
            Ensure(!compact || Math.Abs(scale - Ui.PrimaryHoverScale) <= .005, $"{label} {text}: hover scale is {scale:0.###}");
            Ensure(button.GetVisualAncestors().OfType<Visual>().Where(v => v.ClipToBounds).All(v => v.TransformToVisual(window) is { } m &&
                    new Rect(v.Bounds.Size).TransformToAABB(m).Inflate(.5).Contains(box)),
                $"{label} {text}: no container clips the button at scale {scale:0.###} ({box})");
        }
    }

    // Text links
    private static readonly Color PrimaryHover = Color.Parse("#2F7D5F");
    private static async Task TextLinks(MainWindow window)
    {
        var (width, height) = (window.Width, window.Height);
        foreach (var (w, h) in new[] { (1040d, 720d), (1920d, 1080d) })
        {
            window.Width = w; window.Height = h;
            var size = $"{w:0}x{h:0}";
            window.ShowCollection(MarketplaceView.Cheapest); await Settle();
            var collection = (CollectionView)window.CurrentPage!;
            await TextLink(window, collection.BackLink, () => Fits(collection.Scroll), $"{size} collection");
            window.ShowSearch("roti"); await Settle();
            var search = (CollectionView)window.CurrentPage!;
            await TextLink(window, search.BackLink, () => Fits(search.Scroll), $"{size} search");
            window.ShowCart(); await Settle();
            var cart = (CartView)window.CurrentPage!;
            await TextLink(window, cart.BackLink, () => (cart.ListScroll is not { } list || Fits(list)) && cart.Bounds.Width <= window.Bounds.Width + .5, $"{size} cart");
        }
        window.Width = width; window.Height = height; window.ShowMarketplace(); await Settle();
    }
    private static bool Fits(ScrollViewer scroll) => scroll.Extent.Width <= scroll.Viewport.Width + .5;
    internal static async Task TextLink(Window window, Button link, Func<bool> fits, string label, string text = CollectionView.BackLabel)
    {
        var presenter = Presenter(link);
        var glyph = link.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>().SingleOrDefault();
        var caption = link.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == text);
        Ensure(link.Classes.Contains(CollectionView.TextLinkClass) && link.Bounds.Height >= CollectionView.TextLinkMinHeight - .5,
            $"{label}: {text} uses the shared text link and is {link.Bounds.Height:0.#} high");
        void Colors(Color foreground, string state) =>
            Ensure(Fill(presenter.Background) == Avalonia.Media.Colors.Transparent && Fill(presenter.BorderBrush) == Avalonia.Media.Colors.Transparent &&
                Fill(link.Background) == Avalonia.Media.Colors.Transparent && presenter.BoxShadow.Count == 0 && Math.Abs(Scale(link) - 1) <= .005 &&
                Fill(presenter.Foreground) == foreground && Fill(caption.Foreground) == foreground && (glyph is null || Fill(glyph.Stroke) == foreground) &&
                caption.TextDecorations is null or { Count: 0 },
                $"{label} {state}: transparent with {Fill(presenter.Foreground)} text and {Fill(glyph?.Stroke)} arrow, expected {foreground} ({Fill(presenter.Background)} fill)");
        var box = new Rect(link.Bounds.Size).TransformToAABB(link.TransformToVisual(window)!.Value);
        Rect Inner(Visual visual) => new Rect(visual.Bounds.Size).TransformToAABB(visual.TransformToVisual(window)!.Value).Inflate(1);
        var (marks, icon) = (Inner(caption), glyph is null ? default : Inner(glyph));
        var points = new List<(int X, int Y)>();
        for (var y = (int)Math.Ceiling(box.Y) + 1; y < (int)Math.Floor(box.Bottom) - 1; y++)
            for (var x = (int)Math.Ceiling(box.X) + 1; x < (int)Math.Floor(box.Right) - 1; x++)
                if (!marks.Contains(new Point(x + .5, y + .5)) && !icon.Contains(new Point(x + .5, y + .5))) points.Add((x, y));
        var opacity = link.Opacity; link.Opacity = 0;
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);
        var surface = Snapshot(window);
        link.Opacity = opacity;
        await Hold(link, ":pointerover", false);
        Colors(Primary, "at rest");
        var rest = Snapshot(window);

        await Hold(link, ":pointerover", true);
        Colors(PrimaryHover, "hover");
        var hovered = Snapshot(window);
        var canvas = surface((int)box.X + 2, (int)box.Center.Y);
        var contrast = TopBarVerification.Contrast(PrimaryHover, canvas);
        Ensure(PrimaryHover != Primary && contrast >= 4.5 && Fill(Ui.PrimaryHover) == PrimaryHover,
            $"{label}: hover color {PrimaryHover} differs from {Primary} at {contrast:0.0}:1 on the canvas {canvas}");
        var tinted = points.FirstOrDefault(p => !Near(hovered(p.X, p.Y), surface(p.X, p.Y)));
        Ensure(points.Count > 0 && points.All(p => Near(hovered(p.X, p.Y), surface(p.X, p.Y))),
            $"{label}: hover keeps the canvas behind the link ({hovered(tinted.X, tinted.Y)} at {tinted}, canvas {surface(tinted.X, tinted.Y)})");
        Ensure(Enumerable.Range((int)marks.X, (int)marks.Width).Any(x => Enumerable.Range((int)marks.Y, (int)marks.Height).Any(y => rest(x, y) != hovered(x, y))),
            $"{label}: the label is drawn in a new color on hover");
        Ensure(fits(), $"{label}: no horizontal scrolling while hovered");

        await Hold(link, ":pressed", true);
        Colors(Primary, "pressed");
        var pressed = Snapshot(window);
        Ensure(points.All(p => Near(pressed(p.X, p.Y), surface(p.X, p.Y))), $"{label}: pressed keeps the canvas behind the link");
        await Hold(link, ":pressed", false);
        await Hold(link, ":pointerover", false);
        Colors(Primary, "after leave");

        var ring = TopBarVerification.FocusRing(link);
        await Hold(link, ":focus-visible", true);
        Ensure(ring.BorderBrush is ISolidColorBrush { Color: var color } && color == Primary && ring.IsVisible && ring.BorderThickness.Left > 0,
            $"{label}: :focus-visible shows the primary focus ring");
        await Hold(link, ":focus-visible", false);
    }
    // Filter empty button
    internal static async Task FilterEmptyButton(Window window, OrdersView page, Func<bool> fits, string label)
    {
        await Settle();
        var button = page.EmptyButton ?? throw new InvalidOperationException($"{label}: Lihat semua pesanan is shown");
        var presenter = Presenter(button);
        Ensure(button.Content as string == OrdersView.FilterEmptyAction && button.Classes.Contains(Ui.PrimaryClass) && button.Classes.Contains(Ui.CompactClass) &&
            !button.Classes.Contains(CollectionView.TextLinkClass) && button.CornerRadius == new CornerRadius(6) && button.Padding == new Thickness(16, 11) &&
            button.FontSize == 13 && button.FontWeight == FontWeight.SemiBold,
            $"{label}: Lihat semua pesanan uses the shared compact primary button of Jelajahi menu");
        Ensure(Fill(presenter.Background) != Colors.Transparent && Fill(button.Background) == Primary, $"{label}: Lihat semua pesanan is a filled box ({Fill(presenter.Background)})");
        Ensure(button.Bounds.Height >= 40 - .5, $"{label}: Lihat semua pesanan is {button.Bounds.Height:0.#} high, expected at least 40");
        Rect Box(Visual visual) => new Rect(visual.Bounds.Size).TransformToAABB(visual.TransformToVisual(window)!.Value);
        var (box, heading, state) = (Box(button), Box(page.EmptyHeading!), Box(page.EmptyState!));
        Ensure(Math.Abs(box.Y - heading.Bottom - Ui.Spacing.Section) <= .5 && box.Width < state.Width - .5 && Math.Abs(box.Center.X - state.Center.X) <= 1,
            $"{label}: Lihat semua pesanan sits {box.Y - heading.Bottom:0.#} under the title, centered ({box.Center.X:0.#} vs {state.Center.X:0.#})");
        await PrimaryButton(button, true, page.EmptyState!, window, label);
        await HoverShape(window, button, label);
        Ensure(fits(), $"{label}: no horizontal scrolling");
    }
    // Navbar cart
    private static void NavbarCart(MainWindow window)
    {
        var bar = window.TopBar;
        var glyph = bar.CartGlyph;
        Ensure(ReferenceEquals(glyph.Data, CustomerTopBar.CartIcon) && glyph.Width == 24 && glyph.Height == 24 && glyph.Stretch == Stretch.None &&
            glyph.StrokeThickness == 1.75 && glyph.StrokeLineCap == PenLineCap.Round && glyph.StrokeJoin == PenLineJoin.Round && glyph.Fill is null,
            "Navbar cart keeps its original 24 line icon with stroke 1.75");
        Ensure(bar.CartButton.Width == 44 && bar.CartButton.Height == 44 && bar.CartBadge.Parent == glyph.Parent && bar.CartIndicator.Parent == glyph.Parent,
            "Navbar cart keeps its 44 hit area with the badge and indicator");
    }
    // Empty icons
    internal static async Task EmptyIcon(Window window, Avalonia.Controls.Shapes.Path? icon, Control state, Func<bool> fits, string label)
    {
        await Settle();
        Ensure(icon is { IsEffectivelyVisible: true }, $"{label}: the empty icon is shown");
        var size = Ui.EmptyIconSize;
        Ensure(size is >= 72 and <= 96 && icon!.Width == size && icon.Height == size && Math.Abs(icon.Bounds.Width - size) < .5 && Math.Abs(icon.Bounds.Height - size) < .5,
            $"{label}: the empty icon is {icon!.Bounds.Width:0.#}x{icon.Bounds.Height:0.#}, expected {size}");
        Ensure(Math.Abs(icon.StrokeThickness - size * Ui.EmptyIconStroke) < .01 && icon.StrokeLineCap == PenLineCap.Round && icon.StrokeJoin == PenLineJoin.Round && icon.Fill is null,
            $"{label}: the empty icon is a {icon.StrokeThickness:0.##} thick outline with round caps and joins and no fill");
        Ensure(Fill(icon.Stroke) == Primary && ReferenceEquals(icon.Stroke, Ui.EmptyIconBrush), $"{label}: the empty icon uses the primary token ({Fill(icon.Stroke)})");
        var ink = icon.Data!.GetRenderBounds(new Pen(icon.Stroke, icon.StrokeThickness, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round));
        Ensure(ink.X >= -.5 && ink.Y >= -.5 && ink.Right <= size + .5 && ink.Bottom <= size + .5, $"{label}: the empty icon strokes stay inside its {size} box ({ink})");
        var box = new Rect(icon.Bounds.Size).TransformToAABB(icon.TransformToVisual(window)!.Value);
        var area = new Rect(state.Bounds.Size).TransformToAABB(state.TransformToVisual(window)!.Value);
        Ensure(new Rect(window.Bounds.Size).Contains(box) && area.Contains(box) && icon.GetVisualAncestors().OfType<Visual>().Where(v => v.ClipToBounds)
            .All(v => v.TransformToVisual(window) is { } m && new Rect(v.Bounds.Size).TransformToAABB(m).Contains(box)), $"{label}: the empty icon is not cut off ({box})");
        Ensure(Math.Abs(box.Center.X - area.Center.X) <= 1 && Math.Abs((box.X + ink.Center.X) - area.Center.X) <= size * .05,
            $"{label}: the empty icon is centered in the empty state ({box.Center.X:0.#} vs {area.Center.X:0.#})");
        var pixels = Snapshot(window);
        var painted = 0;
        for (var y = (int)box.Y; y < (int)box.Bottom; y++)
            for (var x = (int)box.X; x < (int)box.Right; x++)
                if (Near(pixels(x, y), Primary)) painted++;
        Ensure(painted > size * size * .08, $"{label}: the empty icon renders in primary ({painted} pixels)");
        Ensure(fits(), $"{label}: no horizontal scrolling");
    }
    // Calendar look
    private static async Task CalendarLook(DateField date, string size)
    {
        var calendar = date.Calendar;
        var sheet = (Avalonia.Controls.Border)date.Popup.Child!;
        var root = sheet.GetVisualAncestors().LastOrDefault() ?? sheet;
        Ensure(Fill(sheet.Background) == Colors.White && Fill(calendar.Background) == Colors.White && Fill(calendar.HeaderBackground) == Colors.White &&
            Fill(calendar.BorderBrush) == Colors.Transparent && sheet.CornerRadius == new CornerRadius(Ui.FieldRadius) && Fill(sheet.BorderBrush) == DividerColor,
            $"{size}: the calendar is white inside the rounded sheet with a thin divider border");
        var titles = calendar.GetVisualDescendants().OfType<TextBlock>().Where(t => Ui.DayNames.Contains(t.Text)).OrderBy(t => t.TransformToVisual(root)!.Value.M31).Select(t => t.Text).ToList();
        Ensure(titles.SequenceEqual(new[] { "Sen", "Sel", "Rab", "Kam", "Jum", "Sab", "Min" }), $"{size}: day names read {string.Join(" ", titles)}, Monday first in Indonesian");
        var header = calendar.GetVisualDescendants().OfType<Button>().First(b => b.Name == Ui.CalendarHeaderPart);
        var month = calendar.DisplayDate.ToString("MMMM yyyy", Ui.Indonesian);
        Ensure(header.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == month) && month.Contains('—') == false,
            $"{size}: the header reads {month} in Indonesian");

        Rect Box(Visual visual) => new Rect(visual.Bounds.Size).TransformToAABB(visual.TransformToVisual(root)!.Value);
        async Task NoGrey(string view)
        {
            var marks = calendar.GetVisualDescendants().Where(v => v is TextBlock or Avalonia.Controls.Shapes.Path).ToList();
            foreach (var mark in marks) mark.Opacity = 0;
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);
            var bare = Snapshot(root);
            foreach (var mark in marks) mark.Opacity = 1;
            var area = Box(calendar);
            var grey = 0;
            for (var y = (int)Math.Ceiling(area.Y); y < (int)area.Bottom; y++)
                for (var x = (int)Math.Ceiling(area.X); x < (int)area.Right; x++)
                {
                    var p = bare(x, y);
                    if (Math.Max(p.R, Math.Max(p.G, p.B)) - Math.Min(p.R, Math.Min(p.G, p.B)) <= 2 && p.R <= 248) grey++;
                }
            Ensure(grey == 0, $"{size}: no default grey pixels in the {view} calendar ({grey} found in {area})");
        }
        await NoGrey("day");

        async Task Hover(Button cell, Color background, Color foreground, string label)
        {
            var presenter = Presenter(cell);
            var rest = Snapshot(root);
            var restBox = Box(cell);
            await Hold(cell, ":pointerover", true);
            var hovered = Snapshot(root);
            var (fill, text, border, thickness) = (Fill(presenter.Background), Fill(cell.Foreground), Fill(presenter.BorderBrush), presenter.BorderThickness);
            var weight = cell.FontWeight;
            await Hold(cell, ":pointerover", false);
            var contrast = TopBarVerification.Contrast(text, fill);
            Ensure(fill == background && text == foreground && fill != Colors.White && contrast >= 4.5,
                $"{label}: hover is {fill} with {text} text ({contrast:0.0}:1), expected {background} and {foreground}");
            if (cell is CalendarButton)
                Ensure(!cell.ClipToBounds && presenter.CornerRadius == new CornerRadius(Ui.FieldGap) && cell.Bounds.Width > cell.Bounds.Height,
                    $"{label}: the hover face is a rounded rectangle ({presenter.CornerRadius}, {cell.Bounds.Width:0.#}x{cell.Bounds.Height:0.#}) and nothing clips it");
            else
                Ensure(!cell.ClipToBounds && presenter.CornerRadius == new CornerRadius(cell.Bounds.Width / 2) && Math.Abs(cell.Bounds.Width - cell.Bounds.Height) < .5,
                    $"{label}: the hover face is a circle ({presenter.CornerRadius}, {cell.Bounds.Width:0.#}x{cell.Bounds.Height:0.#}) and nothing clips it");
            var box = Box(cell);
            var (left, top, right, bottom) = ((int)Math.Ceiling(box.X), (int)Math.Ceiling(box.Y), (int)Math.Floor(box.Right) - 1, (int)Math.Floor(box.Bottom) - 1);
            foreach (var (x, y) in new[] { (left, top), (right, top), (left, bottom), (right, bottom) })
                Ensure(Near(hovered(x, y), rest(x, y)), $"{label}: corner ({x},{y}) keeps the white surface {rest(x, y)}, not a square {hovered(x, y)}");
            var (cx, cy) = ((int)box.Center.X, top + 4);
            Ensure(Near(hovered(cx, cy), background), $"{label}: inside the circle the hover fill is {hovered(cx, cy)}, expected {background}");
            var calendarCell = cell is CalendarDayButton or CalendarButton;
            if (calendarCell)
                Ensure(box == restBox && cell.Bounds.Size == new Size(cell.Width, cell.Height), $"{label}: the hover cell keeps its size and position ({restBox} -> {box})");
            if (calendarCell && background == Selection)
            {
                Ensure(thickness == default(Thickness) && border.A == 0 && weight == FontWeight.Medium,
                    $"{label}: hover has no border and medium text ({thickness}, {border}, {weight})");
                foreach (var (x, y) in new[] { ((int)box.Center.X, top), ((int)box.Center.X, bottom), (left, (int)box.Center.Y), (right, (int)box.Center.Y) })
                    Ensure(!Near(hovered(x, y), Primary500) && !Near(hovered(x, y), Primary), $"{label}: edge ({x},{y}) shows no green outline ({hovered(x, y)})");
            }
            if (calendarCell && background == Primary)
                Ensure(thickness == default(Thickness) && border.A == 0, $"{label}: the selected cell has no extra outline ({thickness}, {border})");
        }
        var days = calendar.GetVisualDescendants().OfType<CalendarDayButton>().Where(b => b.IsEnabled && b.IsVisible).ToList();
        var plain = days.First(b => !b.Classes.Contains(":selected") && !b.Classes.Contains(":today") && !b.Classes.Contains(":inactive"));
        await Hover(plain, Selection, Primary, $"{size} day {plain.Content}");
        if (days.FirstOrDefault(b => b.Classes.Contains(":inactive") && !b.Classes.Contains(":today")) is { } outside)
        {
            var faint = Fill(outside.Foreground);
            Ensure(faint.A < 255 && faint.R == MutedColor.R && faint.G == MutedColor.G && faint.B == MutedColor.B,
                $"{size}: days outside the month use a paler Text Secondary ({faint})");
            await Hover(outside, Selection, Primary, $"{size} outside day {outside.Content}");
        }
        if (days.FirstOrDefault(b => b.Classes.Contains(":selected")) is { } chosen)
            await Hover(chosen, Primary, Colors.White, $"{size} selected day");
        if (days.FirstOrDefault(b => b.Classes.Contains(":today") && !b.Classes.Contains(":selected")) is { } today)
            Ensure(Fill(Presenter(today).BorderBrush) == Primary && Fill(Presenter(today).Background) == Colors.Transparent, $"{size}: today has a thin Primary 700 ring without fill");
        foreach (var name in Ui.CalendarNavParts)
        {
            var nav = calendar.GetVisualDescendants().OfType<Button>().First(b => b.Name == name);
            Ensure(nav.Classes.Contains(Ui.IconClass) && nav.Theme == Ui.HeaderTheme && nav.Width == Ui.CalendarNavSize,
                $"{size}: {name} is a round {Ui.CalendarNavSize} icon button");
            await Hover(nav, Selection, Primary, $"{size} {name}");
        }

        calendar.DisplayMode = CalendarMode.Year; await Settle();
        await NoGrey("month");
        var months = calendar.GetVisualDescendants().OfType<CalendarButton>().Where(b => b.IsVisible).ToList();
        var monthNames = months.Select(b => Presenter(b).Content?.ToString()).ToList();
        Ensure(monthNames.SequenceEqual(Ui.MonthNames), $"{size}: month cells read {string.Join(" ", monthNames)} in Indonesian");
        Ensure(months.All(b => b.Theme == Ui.MonthTheme), $"{size}: month cells use the month theme");
        await Hover(months.First(b => !b.Classes.Contains(":selected") && !b.Classes.Contains(Ui.NowClass)), Selection, Primary, $"{size} month cell");
        if (months.FirstOrDefault(b => b.Classes.Contains(":selected")) is { } shownMonth)
            await Hover(shownMonth, Primary, Colors.White, $"{size} selected month");
        if (months.FirstOrDefault(b => b.Classes.Contains(Ui.NowClass) && !b.Classes.Contains(":selected")) is { } nowMonth)
            Ensure(Fill(Presenter(nowMonth).BorderBrush) == Primary && Fill(Presenter(nowMonth).Background) == Colors.Transparent, $"{size}: this month has a thin Primary 700 ring without fill");

        calendar.DisplayMode = CalendarMode.Decade; await Settle();
        await NoGrey("year");
        var years = calendar.GetVisualDescendants().OfType<CalendarButton>().Where(b => b.IsVisible && b.IsEnabled).ToList();
        Ensure(years.Count > 0 && years.All(b => int.TryParse(Presenter(b).Content?.ToString(), out _)), $"{size}: year cells show plain years");
        await Hover(years.First(b => !b.Classes.Contains(":selected") && !b.Classes.Contains(Ui.NowClass) && !b.Classes.Contains(":inactive")), Selection, Primary, $"{size} year cell");
        if (years.FirstOrDefault(b => b.Classes.Contains(":selected")) is { } shownYear)
            await Hover(shownYear, Primary, Colors.White, $"{size} selected year");
        calendar.DisplayMode = CalendarMode.Month; await Settle();
    }
    private static bool Near(Color a, Color b) => Math.Abs(a.R - b.R) <= 3 && Math.Abs(a.G - b.G) <= 3 && Math.Abs(a.B - b.B) <= 3;
    private static Func<int, int, Color> Snapshot(Visual window)
    {
        var size = new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height);
        using var bitmap = new RenderTargetBitmap(size, new Vector(96, 96));
        bitmap.Render(window);
        using var target = new WriteableBitmap(size, new Vector(96, 96), Avalonia.Platform.PixelFormat.Bgra8888, Avalonia.Platform.AlphaFormat.Unpremul);
        var bytes = new byte[size.Width * size.Height * 4];
        using (var frame = target.Lock())
        {
            bitmap.CopyPixels(frame);
            for (var row = 0; row < size.Height; row++)
                System.Runtime.InteropServices.Marshal.Copy(frame.Address + row * frame.RowBytes, bytes, row * size.Width * 4, size.Width * 4);
        }
        return (x, y) => { var i = (y * size.Width + x) * 4; return Color.FromArgb(bytes[i + 3], bytes[i + 2], bytes[i + 1], bytes[i]); };
    }
    private static void Capture(Window window, string path)
    {
        using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height), new Vector(96, 96));
        bitmap.Render(window); bitmap.Save(path, PngBitmapEncoderOptions.Default);
    }
    private static void Ensure(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
