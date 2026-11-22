using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Replate.Domain.Services;

namespace Replate.Desktop;

internal sealed class RestaurantProfileView : UserControl
{
    internal TextBox AddressInput { get; }
    internal TextBox PhoneInput { get; }
    internal Button SaveButton { get; }
    internal Control? LocationMap { get; }
    internal Dictionary<string, TextBlock> FieldErrors { get; } = [];
    private readonly Dictionary<string, (Control Focus, Border Outline)> fields = [];
    private readonly TextBlock formError = Ui.FieldError();
    private readonly MainWindow host;
    private readonly DemoSession session;
    private readonly TextBox nameInput;
    private readonly TextBox descriptionInput;
    private readonly TimeField openingInput;
    private readonly TimeField closingInput;

    internal RestaurantProfileView(MainWindow host, DemoSession session)
    {
        this.host = host;
        this.session = session;
        var restaurant = session.Restaurant;
        nameInput = Ui.Input("Nama restoran", restaurant.Name);
        AddressInput = Ui.Input("Alamat restoran", restaurant.Address);
        PhoneInput = Ui.Input("Nomor telepon", restaurant.Phone);
        descriptionInput = Ui.Input("Deskripsi restoran", restaurant.Description, multiline: true);
        openingInput = new TimeField("Jam buka") { Value = restaurant.OpeningTime };
        closingInput = new TimeField("Jam tutup") { Value = restaurant.ClosingTime };
        SaveButton = Ui.Button("Simpan profil", () => { }, primary: true);
        SaveButton.HorizontalAlignment = HorizontalAlignment.Left;
        SaveButton.Click += async (_, _) => await SaveAsync();
        formError.IsVisible = false;

        var form = Ui.Stack(Ui.Spacing.Related,
            Field("restaurantName", "Nama restoran", nameInput),
            Field("address", "Alamat", AddressInput),
            Field("phone", "Telepon", PhoneInput),
            Ui.FormPair(Field("openingTime", "Jam buka", openingInput), Field("closingTime", "Jam tutup", closingInput)),
            Field("description", "Deskripsi", descriptionInput), formError, SaveButton);

        if (restaurant.Location is { } location) LocationMap = MapView.Create(location, [new(location, false)], 232);
        var mapCard = new Border
        {
            Child = Ui.Stack(12, Ui.Text("Lokasi di peta", 20, bold: true),
                LocationMap ?? Ui.Text(MapView.Unavailable, 14, Ui.Muted)),
            Background = Brushes.White, CornerRadius = new CornerRadius(16), BoxShadow = Ui.CardShadow,
            Padding = new Thickness(20), VerticalAlignment = VerticalAlignment.Top
        };
        var columns = Ui.Columns("*,*", form, mapCard);
        columns.ColumnSpacing = Ui.Spacing.Page;
        Content = Ui.Page(Ui.Text("Profil restoran", MarketplaceView.TitleSize, bold: true), columns);
    }

    private Control Field(string key, string label, Control input)
    {
        var error = Ui.FieldError();
        error.IsVisible = false;
        var outline = new Border { Child = input, BorderBrush = Brushes.Transparent, BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(input is TimeField ? 22 : 6) };
        fields.Add(key, (input is TimeField time ? time.Hour : input, outline));
        FieldErrors.Add(key, error);
        AutomationProperties.SetName(input, label);
        return Ui.Stack(8, Ui.Text(label, 14, bold: true), outline, error);
    }

    private async Task SaveAsync()
    {
        if (!SaveButton.IsEnabled) return;
        SaveButton.IsEnabled = false;
        formError.IsVisible = false;
        ShowErrors(null);
        try
        {
            var profile = new RestaurantProfile(nameInput.Text ?? "", AddressInput.Text ?? "", PhoneInput.Text ?? "",
                openingInput.Value, closingInput.Value, descriptionInput.Text ?? "");
            await session.SaveRestaurantProfileAsync(profile);
            if (host.CurrentPage != this) return;
            host.ShowRestaurantProfile();
            host.ShowSuccess(session.LastGeocodeWarning ?? "Profil restoran tersimpan.");
        }
        catch (ProfileValidationException error) { ShowErrors(error.Errors); }
        catch (Exception error) when (error is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            formError.Text = error is InvalidOperationException ? error.Message : "Profil restoran tidak dapat disimpan.";
            formError.IsVisible = true;
        }
        finally { SaveButton.IsEnabled = true; }
    }

    private void ShowErrors(IReadOnlyDictionary<string, string>? errors)
    {
        Control? firstInvalid = null;
        foreach (var (key, (focus, outline)) in fields)
        {
            var message = errors?.GetValueOrDefault(key);
            var error = FieldErrors[key];
            error.Text = message;
            error.IsVisible = message is not null;
            outline.BorderBrush = error.IsVisible ? Ui.Danger : Brushes.Transparent;
            AutomationProperties.SetHelpText(focus, message ?? "");
            if (error.IsVisible) firstInvalid ??= focus;
        }
        firstInvalid?.Focus();
    }
}
