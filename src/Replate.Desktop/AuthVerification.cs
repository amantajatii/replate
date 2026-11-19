using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using Replate.Domain.Entities;
using Replate.Domain.Services;

namespace Replate.Desktop;

internal static class AuthVerification
{
    internal static async Task Run(string output)
    {
        void Ensure(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
        void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        async Task Settle() => await Task.Delay(300);
        var path = Path.Combine(Path.GetTempPath(), "replate-auth-" + Guid.NewGuid().ToString("N"), "state.json");
        var session = new DemoSession(path);
        var window = new MainWindow(session) { Width = 1040, Height = 720 };
        try
        {
            window.Show(); await Settle();
            void Shot(string name)
            {
                using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height), new Vector(96, 96));
                bitmap.Render(window); bitmap.Save(Path.Combine(output, name + ".png"), PngBitmapEncoderOptions.Default);
            }
            IEnumerable<TextBlock> Texts() => window.GetVisualDescendants().OfType<TextBlock>().Where(t => t.IsVisible);
            Button Find(string text) => window.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == text);
            SignInView SignInPage() => window.CurrentPage as SignInView ?? throw new InvalidOperationException("Sign-in page expected");
            async Task SignIn(string email, string password, bool enter = false)
            {
                var page = SignInPage();
                page.EmailInput.Text = email; page.PasswordInput.Text = password;
                if (enter)
                {
                    page.PasswordInput.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
                }
                else Click(page.SignInButton);
                await Settle();
            }
            async Task SignOut()
            {
                Click(Find("Keluar")); await Settle();
                Ensure(window.CurrentPage is SignInView && !session.IsSignedIn, "Keluar signs out and returns to sign-in");
                Ensure(!window.GetVisualDescendants().OfType<Button>().Any(b => b.Content as string is "Dashboard" or "Jelajah"),
                    "Signed-out window shows no area navigation");
            }

            // Sign-in screen
            var signIn = SignInPage();
            Ensure(window.FocusManager?.GetFocusedElement() == signIn.EmailInput, "Initial focus is Email");
            Ensure(signIn.PasswordInput.PasswordChar != default, "Password is masked");
            Ensure(Avalonia.Automation.AutomationProperties.GetName(signIn.EmailInput) == "Email" &&
                Avalonia.Automation.AutomationProperties.GetName(signIn.PasswordInput) == "Kata sandi", "Sign-in automation names match labels");
            Ensure(Texts().Any(t => t.Text == "Masuk") && Texts().Any(t => t.Text == "replate.") &&
                Texts().Any(t => t.Text == "Akun demo: pelanggan@replate.demo atau dapur-bumi@replate.demo, kata sandi replate123."),
                "Sign-in shows wordmark, heading and demo account hint");
            Ensure(!window.GetVisualDescendants().OfType<Button>().Any(b => b.Content as string is "Jelajahi makanan" or "Buka area restoran" or "Ganti area"),
                "Old automatic demo entry buttons are removed");
            Ensure(((Avalonia.Media.ISolidColorBrush)signIn.SignInButton.Background!).Color == Avalonia.Media.Color.Parse("#245B43"), "Masuk is the green primary action");
            Shot("auth-sign-in");
            await SignIn(DemoAccounts.CustomerEmail, "salah-sekali", true);
            Ensure(window.CurrentPage == signIn && !session.IsSignedIn && signIn.ErrorText.IsVisible &&
                signIn.ErrorText.Text == AccountService.SignInFailedMessage, "Wrong password shows generic message and stays on sign-in");
            Ensure(signIn.PasswordInput.Text == "" && signIn.EmailInput.Text == DemoAccounts.CustomerEmail, "Password cleared after submission");
            Shot("auth-sign-in-error");
            await SignIn("tidak-ada@replate.demo", "replate123");
            Ensure(window.CurrentPage == signIn && signIn.ErrorText.Text == AccountService.SignInFailedMessage, "Unknown email uses the same generic message");

            // Demo accounts
            await SignIn(DemoAccounts.OwnerEmail("Dapur Bumi"), DemoAccounts.Password, true);
            Ensure(window.CurrentPage is DashboardView && session.CurrentUser is RestaurantOwner && session.Restaurant.Name == "Dapur Bumi",
                "Demo owner sign-in opens dashboard");
            Ensure(Texts().Any(t => t.Text == "Dapur Bumi"), "Restaurant rail shows owner's restaurant");
            await SignOut();
            await SignIn(DemoAccounts.CustomerEmail, DemoAccounts.Password);
            Ensure(window.CurrentPage is MarketplaceView && session.CurrentUser is Customer, "Demo customer sign-in opens marketplace");
            await SignOut();

            // Role guards
            var listing = session.Listings[0];
            var bumiOrder = session.FindRestaurant(listing.Menu.RestaurantId)!.Orders[0];
            var customerRoutes = new (string Name, Action Show)[]
            {
                ("ShowMarketplace", window.ShowMarketplace), ("ShowFood", () => window.ShowFood(listing)), ("ShowCart", window.ShowCart),
                ("ShowCustomerOrders", window.ShowCustomerOrders), ("ShowOrder(customer)", () => window.ShowOrder(bumiOrder, false))
            };
            var ownerRoutes = new (string Name, Action Show)[]
            {
                ("ShowDashboard", window.ShowDashboard), ("ShowMenus", window.ShowMenus), ("ShowMenuEditor", () => window.ShowMenuEditor()),
                ("ShowListings", window.ShowListings), ("ShowListingEditor", () => window.ShowListingEditor()), ("ShowProduction", window.ShowProduction),
                ("ShowRestaurantOrders", window.ShowRestaurantOrders), ("ShowOrder(restaurant)", () => window.ShowOrder(bumiOrder, true))
            };
            void Guarded(Action signIn, (string Name, Action Show)[] routes, string role)
            {
                foreach (var route in routes)
                {
                    signIn();
                    route.Show();
                    Ensure(window.CurrentPage is SignInView && !session.IsSignedIn, $"{route.Name} redirects {role} to sign-in");
                }
            }
            Guarded(() => { }, customerRoutes.Concat(ownerRoutes).ToArray(), "signed-out user");
            Guarded(() => session.AsOwner(), customerRoutes, "owner");
            Guarded(() => session.AsCustomer(), ownerRoutes, "customer");
            Guarded(() => session.AsOwner("Warung Sambal Ijo"), new (string, Action)[]
            {
                ("ShowOrder(other restaurant)", () => window.ShowOrder(bumiOrder, true)),
                ("ShowMenuEditor(other restaurant)", () => window.ShowMenuEditor(listing.Menu)),
                ("ShowListingEditor(other restaurant)", () => window.ShowListingEditor(listing))
            }, "other owner");

            // Registration validation
            Click(SignInPage().RegisterButton); await Settle();
            var register = window.CurrentPage as RegisterView ?? throw new InvalidOperationException("Daftar akun baru opens registration");
            Ensure(window.FocusManager?.GetFocusedElement() == register.NameInput, "Registration focuses Nama");
            Ensure(register.CustomerOption.IsChecked == true && !register.ProfileSection.IsVisible, "Customer is default and hides restaurant profile");
            var users = session.Users.Count;
            register.NameInput.Text = "Pelanggan Uji";
            register.EmailInput.Text = DemoAccounts.CustomerEmail.ToUpperInvariant();
            register.PasswordInput.Text = "pendek"; register.ConfirmPasswordInput.Text = "berbeda";
            Click(register.RegisterButton); await Settle();
            Ensure(register.FieldErrors["email"].IsVisible && register.FieldErrors["email"].Text == "Email sudah terdaftar.", "Duplicate email shown inline");
            Ensure(register.FieldErrors["password"].Text == $"Kata sandi minimal {AccountService.MinimumPasswordLength} karakter.", "Short password shown inline");
            Ensure(register.FieldErrors["confirmPassword"].Text == "Kata sandi tidak sama.", "Mismatched confirmation shown inline");
            Ensure(!register.FieldErrors["restaurantName"].IsVisible, "Customer registration does not validate restaurant profile");
            Ensure(window.CurrentPage == register && session.Users.Count == users && !session.IsSignedIn, "Invalid registration creates nothing");
            Ensure(window.FocusManager?.GetFocusedElement() == register.EmailInput, "Focus first invalid registration field");
            Shot("auth-register-errors");
            register.OwnerOption.IsChecked = true; await Settle();
            Ensure(register.ProfileSection.IsVisible, "Owner role shows restaurant profile");
            foreach (var (key, message) in new[] { ("restaurantName", "Nama restoran wajib diisi."), ("address", "Alamat restoran wajib diisi."),
                ("phone", "Nomor telepon wajib diisi."), ("openingTime", "Jam buka wajib diisi."), ("closingTime", "Jam tutup wajib diisi.") })
                Ensure(register.FieldErrors[key].IsVisible && register.FieldErrors[key].Text == message, $"Empty profile error for {key} shown inline");
            foreach (var (input, label) in new (Control, string)[] { (register.NameInput, "Nama"), (register.EmailInput, "Email"),
                (register.PasswordInput, "Kata sandi"), (register.ConfirmPasswordInput, "Ulangi kata sandi"), (register.RestaurantNameInput, "Nama restoran"),
                (register.AddressInput, "Alamat"), (register.PhoneInput, "Telepon"), (register.DescriptionInput, "Deskripsi"),
                (register.OpeningInput, "Jam buka"), (register.ClosingInput, "Jam tutup"), (register.CustomerOption, "Pelanggan"), (register.OwnerOption, "Pemilik restoran") })
                Ensure(Avalonia.Automation.AutomationProperties.GetName(input) == label && (input is RadioButton || Texts().Any(t => t.Text == label)),
                    $"{label} has visible label and matching automation name");
            Ensure(Texts().Any(t => t.Text == "Untuk buka 24 jam, isi 00:00 sampai 23:59."), "24-hour helper text shown");
            Shot("auth-register-owner-errors");
            register.CustomerOption.IsChecked = true; await Settle();
            Ensure(!register.ProfileSection.IsVisible && !register.FieldErrors["restaurantName"].IsVisible, "Switching back hides profile errors");

            // Customer registration
            register.EmailInput.Text = "pelanggan-baru@replate.demo";
            register.PasswordInput.Text = "rahasia123"; register.ConfirmPasswordInput.Text = "rahasia123";
            Ensure(register.FieldErrors.Values.All(e => !e.IsVisible), "Corrected registration fields clear errors");
            Click(register.RegisterButton); await Settle();
            Ensure(window.CurrentPage is MarketplaceView && session.CurrentUser is Customer { Email: "pelanggan-baru@replate.demo" } &&
                window.SuccessMessage == "Akun berhasil dibuat.", "New customer registration signs in to marketplace with notification");
            Ensure(!session.CustomerOrders.Any() && session.Cart.Items.Count == 0, "New customer starts with empty cart and orders");
            await SignOut();

            // Owner registration
            Click(SignInPage().RegisterButton); await Settle();
            register = (RegisterView)window.CurrentPage!;
            register.OwnerOption.IsChecked = true;
            register.NameInput.Text = "Pemilik Uji"; register.EmailInput.Text = "kedai-uji@replate.demo";
            register.PasswordInput.Text = "rahasia123"; register.ConfirmPasswordInput.Text = "rahasia123";
            register.RestaurantNameInput.Text = "Kedai Uji Baru"; register.AddressInput.Text = "Jl. Uji No. 1, Yogyakarta";
            register.PhoneInput.Text = "0274 555 0909"; register.DescriptionInput.Text = "Restoran untuk pemeriksaan.";
            register.OpeningInput.SelectedTime = new TimeSpan(0, 0, 0); register.ClosingInput.SelectedTime = new TimeSpan(23, 59, 0);
            Shot("auth-register-owner");
            Click(register.RegisterButton); await Settle();
            Ensure(window.CurrentPage is DashboardView && session.CurrentUser is RestaurantOwner && session.Restaurant.Name == "Kedai Uji Baru" &&
                window.SuccessMessage == "Akun berhasil dibuat.", "New owner registration opens own dashboard");
            var created = session.Restaurant;
            Ensure(created.Menus.Count == 0 && created.Orders.Count == 0 && !session.RestaurantListings.Any(), "New restaurant starts empty");
            Ensure(created.OpeningTime == new TimeOnly(0, 0) && created.ClosingTime == new TimeOnly(23, 59), "Opening hours saved");
            Ensure(Texts().Any(t => t.Text == "Kedai Uji Baru") && !Texts().Any(t => t.Text == "Dapur Bumi"), "Rail shows the new restaurant name");
            async Task CheckEmptyPage<T>(Action show, string title, string action) where T : Control
            {
                show(); await Settle();
                Ensure(window.CurrentPage is T && session.CurrentUser is RestaurantOwner, $"{typeof(T).Name} opens for new restaurant");
                Ensure(Texts().Any(t => t.Text == title) && window.GetVisualDescendants().OfType<Button>().Any(b => b.IsVisible && b.Content as string == action),
                    $"{typeof(T).Name} has empty state with {action}");
                Ensure(!Texts().Any(t => t.Text is { } text && (text.Contains("NaN") || text.Contains('∞') || text.Contains("Dapur Bumi") ||
                    text.Contains("Chicken rice bowl") || text.Contains("Restoran ini bukan milik"))), $"{typeof(T).Name} shows no invalid numbers or other restaurant data");
            }
            await CheckEmptyPage<DashboardView>(window.ShowDashboard, "Belum ada menu", "Tambah menu");
            Ensure(Texts().Any(t => t.Text == "Belum ada produksi pada periode ini") && Texts().Any(t => t.Text == "Belum ada surplus pada periode ini") &&
                !Texts().Any(t => t.Text?.Contains('%') == true), "Empty dashboard avoids meaningless percentages");
            Shot("auth-new-owner-dashboard");
            await CheckEmptyPage<MenusView>(window.ShowMenus, "Belum ada menu. Tambahkan menu untuk mulai mencatat produksi.", "Tambah menu");
            await CheckEmptyPage<ListingsView>(window.ShowListings, "Belum ada penawaran. Catat produksi, lalu tawarkan surplus yang tersedia.", "Buat penawaran");
            await CheckEmptyPage<ListingEditorView>(() => window.ShowListingEditor(), "Tambahkan menu aktif terlebih dahulu.", "Tambah menu");
            await CheckEmptyPage<ProductionView>(window.ShowProduction, "Tambahkan menu sebelum mencatat produksi.", "Tambah menu");
            await CheckEmptyPage<OrdersView>(window.ShowRestaurantOrders, "Belum ada pesanan", "Buat penawaran");
            Shot("auth-new-owner-orders");
            Click(Find("Buat penawaran")); await Settle();
            Ensure(window.CurrentPage is ListingEditorView, "Orders empty state leads to listing creation");
            window.ShowDashboard(); ((DashboardView)window.CurrentPage!).PeriodPicker.SelectedIndex = 0; await Settle();
            Ensure(window.CurrentPage is DashboardView && Texts().Any(t => t.Text == "Belum ada menu"), "Empty dashboard survives period change");
            Click(Find("Tambah menu")); await Settle();
            Ensure(window.CurrentPage is MenuEditorView, "Dashboard empty state leads to menu creation");
            var newMenu = session.SaveMenu(null, "Nasi uji", "", "Makanan utama", 15000, .3m);
            window.ShowDashboard(); await Settle();
            Ensure(Texts().Any(t => t.Text == "Surplus per menu") && Texts().Any(t => t.Text == "Belum ada data") &&
                !Texts().Any(t => t.Text?.Contains("NaN") == true), "Menu without production shows rows without invalid numbers");
            window.ShowProduction(); await Settle();
            Ensure(window.CurrentPage is ProductionView production && production.MenuInput.SelectedItem == newMenu, "Production opens with new menu");
            await SignOut();

            // Isolation between owners
            await SignIn(DemoAccounts.OwnerEmail("Dapur Bumi"), DemoAccounts.Password);
            Ensure(session.Restaurant.Name == "Dapur Bumi" && !session.Restaurant.Menus.Contains(newMenu), "Other owner keeps own restaurant");
            window.ShowMenus(); await Settle();
            Ensure(!Texts().Any(t => t.Text is "Nasi uji" or "Kedai Uji Baru"), "New owner's data is hidden from other owner");
            window.ShowMenuEditor(newMenu);
            Ensure(window.CurrentPage is SignInView && !session.IsSignedIn, "Other owner cannot open the new owner's menu");
            await SignIn(DemoAccounts.CustomerEmail, DemoAccounts.Password);
            Ensure(window.Marketplace.VisibleCount > 0 && !Texts().Any(t => t.Text == "Nasi uji"), "Menu without listing is not in marketplace");
            await SignOut();

            // Persistence
            var reloaded = new DemoSession(path);
            Ensure(reloaded.StorageError is null && reloaded.SignIn("kedai-uji@replate.demo", "rahasia123") is RestaurantOwner &&
                reloaded.Restaurant.Name == "Kedai Uji Baru" && reloaded.Restaurant.Menus.Single().Name == "Nasi uji", "Registered owner survives reload");
            Ensure(reloaded.SignIn("pelanggan-baru@replate.demo", "rahasia123") is Customer, "Registered customer survives reload");
            Console.WriteLine("All Replate sign-in checks passed (sign-in, demo accounts, wrong password, registration validation, new customer/owner, empty restaurant pages, Keluar, role guards, owner isolation, reload).");
        }
        finally { window.Close(); }
    }
}
