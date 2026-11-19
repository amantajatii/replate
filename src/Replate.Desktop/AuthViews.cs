using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Replate.Domain.Enums;
using Replate.Domain.Services;

namespace Replate.Desktop;

public sealed class SignInView : UserControl
{
    internal TextBox EmailInput { get; }
    internal TextBox PasswordInput { get; }
    internal Button SignInButton { get; }
    internal Button RegisterButton { get; }
    internal TextBlock ErrorText { get; } = Ui.Text("", 14, Brush.Parse("#963E28"));

    internal SignInView(MainWindow host, DemoSession session, string? startupError)
    {
        ErrorText.IsVisible = false;
        EmailInput = Ui.Input();
        PasswordInput = Ui.Input();
        PasswordInput.PasswordChar = '•'; PasswordInput.TextWrapping = TextWrapping.NoWrap;
        void Submit()
        {
            var email = EmailInput.Text ?? "";
            var password = PasswordInput.Text ?? "";
            PasswordInput.Text = "";
            host.Run(() => session.SignIn(email, password), host.ShowHome, onError: message =>
            {
                ErrorText.Text = message; ErrorText.IsVisible = true;
                PasswordInput.Focus();
            });
        }
        PasswordInput.AddHandler(KeyDownEvent, (_, e) =>
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            Submit();
        }, RoutingStrategies.Tunnel);
        SignInButton = Ui.Button("Masuk", Submit, true);
        RegisterButton = Ui.Button("Daftar akun baru", host.ShowRegister);
        var form = Ui.Stack(20, Ui.Field("Email", EmailInput), Ui.Field("Kata sandi", PasswordInput),
            Ui.Row(12, SignInButton, RegisterButton),
            Ui.Text("Akun demo: pelanggan@replate.demo atau dapur-bumi@replate.demo, kata sandi replate123.", 12, Ui.Muted));
        var body = Ui.Stack(24, Ui.Text("replate.", 36, Ui.Green, true));
        if (startupError is not null)
            body.Children.Add(new Border
            {
                Child = Ui.Text("Database belum tersambung. Aplikasi tetap terbuka, tetapi pesanan tidak dapat disimpan sampai PostgreSQL diperbaiki.\n\n" + startupError, 14, Brush.Parse("#7A4E00")),
                Background = Brush.Parse("#FFF4D6"), BorderBrush = Brush.Parse("#E5B94A"), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6), Padding = new Thickness(14)
            });
        body.Children.Add(Ui.Text("Masuk", 28, bold: true));
        body.Children.Add(ErrorText);
        body.Children.Add(Ui.Form(form, 640));
        body.Margin = new Thickness(32);
        Content = Ui.Scroll(body);
        Loaded += (_, _) => EmailInput.Focus();
    }
}

public sealed class RegisterView : UserControl
{
    internal RadioButton CustomerOption { get; }
    internal RadioButton OwnerOption { get; }
    internal TextBox NameInput { get; }
    internal TextBox EmailInput { get; }
    internal TextBox PasswordInput { get; }
    internal TextBox ConfirmPasswordInput { get; }
    internal TextBox RestaurantNameInput { get; }
    internal TextBox AddressInput { get; }
    internal TextBox PhoneInput { get; }
    internal TextBox DescriptionInput { get; }
    internal TimePicker OpeningInput { get; }
    internal TimePicker ClosingInput { get; }
    internal StackPanel ProfileSection { get; }
    internal Button RegisterButton { get; }
    internal Button BackButton { get; }
    internal TextBlock ErrorText { get; } = Ui.Text("", 14, Brush.Parse("#963E28"));
    internal Dictionary<string, TextBlock> FieldErrors { get; } = new();

    internal RegisterView(MainWindow host, DemoSession session)
    {
        ErrorText.IsVisible = false;
        CustomerOption = new RadioButton { Content = "Pelanggan", GroupName = "register-role", IsChecked = true };
        OwnerOption = new RadioButton { Content = "Pemilik restoran", GroupName = "register-role" };
        Avalonia.Automation.AutomationProperties.SetName(CustomerOption, "Pelanggan");
        Avalonia.Automation.AutomationProperties.SetName(OwnerOption, "Pemilik restoran");
        var role = Ui.Row(24, CustomerOption, OwnerOption);
        Avalonia.Automation.AutomationProperties.SetName(role, "Daftar sebagai");
        NameInput = Ui.Input(); EmailInput = Ui.Input();
        PasswordInput = Ui.Input(); ConfirmPasswordInput = Ui.Input();
        foreach (var secret in new[] { PasswordInput, ConfirmPasswordInput })
        { secret.PasswordChar = '•'; secret.TextWrapping = TextWrapping.NoWrap; }
        RestaurantNameInput = Ui.Input(); AddressInput = Ui.Input(); PhoneInput = Ui.Input();
        DescriptionInput = Ui.Input("", true);
        OpeningInput = new TimePicker { ClockIdentifier = "24HourClock" };
        ClosingInput = new TimePicker { ClockIdentifier = "24HourClock" };
        var inputs = new Dictionary<string, Control>
        {
            ["name"] = NameInput, ["email"] = EmailInput, ["password"] = PasswordInput, ["confirmPassword"] = ConfirmPasswordInput,
            ["restaurantName"] = RestaurantNameInput, ["address"] = AddressInput, ["phone"] = PhoneInput,
            ["description"] = DescriptionInput, ["openingTime"] = OpeningInput, ["closingTime"] = ClosingInput
        };
        var outlines = new Dictionary<string, Border>();
        var red = Brush.Parse("#B3261E");
        var attempted = false;
        Control Field(string key, string label)
        {
            inputs[key].HorizontalAlignment = inputs[key] is TimePicker ? HorizontalAlignment.Left : HorizontalAlignment.Stretch;
            var error = Ui.FieldError(); error.IsVisible = false;
            FieldErrors[key] = error;
            var outline = new Border { Child = inputs[key], CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(1), BorderBrush = Brushes.Transparent };
            outline.HorizontalAlignment = inputs[key].HorizontalAlignment;
            outlines[key] = outline;
            Avalonia.Automation.AutomationProperties.SetName(inputs[key], label);
            return Ui.Stack(8, Ui.Text(label, 14, bold: true), outline, error);
        }
        bool IsOwner() => OwnerOption.IsChecked == true;
        static TimeOnly? Time(TimePicker picker) => picker.SelectedTime is { } time ? TimeOnly.FromTimeSpan(time) : null;
        RestaurantProfile Profile() => new(RestaurantNameInput.Text ?? "", AddressInput.Text ?? "", PhoneInput.Text ?? "",
            Time(OpeningInput), Time(ClosingInput), DescriptionInput.Text ?? "");
        Dictionary<string, string> Errors()
        {
            var password = PasswordInput.Text ?? "";
            var errors = session.ValidateRegistration(IsOwner() ? UserRole.RestaurantOwner : UserRole.Customer,
                NameInput.Text ?? "", EmailInput.Text ?? "", password, IsOwner() ? Profile() : null);
            var confirm = ConfirmPasswordInput.Text ?? "";
            if (confirm.Length == 0) { if (password.Length > 0) errors["confirmPassword"] = "Ulangi kata sandi wajib diisi."; }
            else if (confirm != password) errors["confirmPassword"] = "Kata sandi tidak sama.";
            return errors;
        }
        void Display(Dictionary<string, string> errors)
        {
            foreach (var pair in inputs)
            {
                var invalid = errors.TryGetValue(pair.Key, out var message);
                FieldErrors[pair.Key].Text = message ?? "";
                FieldErrors[pair.Key].IsVisible = invalid;
                outlines[pair.Key].BorderBrush = invalid ? red : Brushes.Transparent;
                Avalonia.Automation.AutomationProperties.SetHelpText(pair.Value, message ?? "");
            }
        }
        void Revalidate() { if (attempted) Display(Errors()); }
        RegisterButton = Ui.Button("Daftar", () =>
        {
            attempted = true;
            ErrorText.IsVisible = false;
            var errors = Errors(); Display(errors);
            if (errors.Count > 0)
            {
                var first = inputs.First(p => errors.ContainsKey(p.Key)).Value;
                first.BringIntoView(); first.Focus();
                return;
            }
            var owner = IsOwner();
            var email = EmailInput.Text ?? "";
            var password = PasswordInput.Text ?? "";
            host.Run(() =>
            {
                session.Register(owner ? UserRole.RestaurantOwner : UserRole.Customer, NameInput.Text ?? "", email, password, owner ? Profile() : null);
                session.SignIn(email, password);
            }, host.ShowHome, "Akun berhasil dibuat.", message => { ErrorText.Text = message; ErrorText.IsVisible = true; ErrorText.BringIntoView(); });
        }, true);
        BackButton = Ui.Button("Kembali ke masuk", host.ShowSignIn);
        ProfileSection = Ui.Stack(20, Ui.Text("Profil restoran", 20, bold: true),
            Field("restaurantName", "Nama restoran"), Field("address", "Alamat"), Field("phone", "Telepon"),
            Field("description", "Deskripsi"),
            Ui.FormPair(Field("openingTime", "Jam buka"), Field("closingTime", "Jam tutup")),
            Ui.SupportingText("Untuk buka 24 jam, isi 00:00 sampai 23:59."));
        ProfileSection.IsVisible = false;
        var form = Ui.Stack(20, Ui.Stack(8, Ui.Text("Daftar sebagai", 14, bold: true), role),
            Field("name", "Nama"), Field("email", "Email"), Field("password", "Kata sandi"), Field("confirmPassword", "Ulangi kata sandi"),
            ProfileSection, Ui.Row(12, RegisterButton, BackButton));
        OwnerOption.IsCheckedChanged += (_, _) => { ProfileSection.IsVisible = IsOwner(); Revalidate(); };
        foreach (var pair in inputs)
            pair.Value.PropertyChanged += (_, e) =>
            {
                if (e.Property == TextBox.TextProperty || e.Property == TimePicker.SelectedTimeProperty) Revalidate();
            };
        var body = Ui.Stack(24, Ui.Text("replate.", 36, Ui.Green, true),
            Ui.Stack(6, Ui.Text("Daftar", 28, bold: true),
                Ui.Text($"Kata sandi minimal {AccountService.MinimumPasswordLength} karakter. Pemilik restoran mengisi profil restoran.", 14, Ui.Muted)),
            ErrorText, Ui.Form(form, 640));
        body.Margin = new Thickness(32);
        Content = Ui.Scroll(body);
        Loaded += (_, _) => NameInput.Focus();
    }
}
