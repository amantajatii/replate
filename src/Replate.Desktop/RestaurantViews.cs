using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Replate.Domain.Entities;
using Replate.Domain.Enums;
using Menu = Replate.Domain.Entities.Menu;

namespace Replate.Desktop;

public sealed class MenusView : UserControl
{
    internal MenusView(MainWindow host, DemoSession session)
    {
        Background = Ui.WorkspaceFill;
        Styles.Add(Ui.FocusStyles());
        var list = Ui.Stack(Ui.Spacing.Related);
        foreach (var menu in session.RestaurantMenus)
        {
            var description = Ui.Text(menu.Description, 13, Ui.Muted);
            description.MaxLines = 2;
            description.TextTrimming = TextTrimming.WordEllipsis;
            description.IsVisible = !string.IsNullOrWhiteSpace(menu.Description);
            var details = Ui.Stack(4, Ui.Text(menu.Name, 18, bold: true),
                Ui.Text($"{menu.Category} · {menu.WeightPerPortion * 1000:0.##} g per porsi", 13, Ui.Muted), description);
            var facts = RestaurantListCard.Price("Harga normal", menu.NormalPrice,
                RestaurantListCard.Status(menu.IsActive ? "Aktif" : "Nonaktif", menu.IsActive));
            var toggle = menu.IsActive ? "Nonaktifkan" : "Aktifkan";
            var actions = Ui.Row(8,
                RestaurantListCard.Action("Ubah", menu.Name, () => host.ShowMenuEditor(menu), Ui.Selection, Ui.Green),
                RestaurantListCard.Action(toggle, menu.Name, () => host.Run(() => session.ToggleMenu(menu), host.ShowMenus)),
                RestaurantListCard.Action("Hapus", menu.Name, async () => await MenuDeletionDialog.Show(host, session, menu), foreground: Ui.Danger));
            actions.MinWidth = 224;
            list.Children.Add(new RestaurantListCard(session.PhotoPath(menu.ImageUrl), details, facts, actions));
        }
        if (list.Children.Count == 0) list.Children.Add(Ui.Text("Belum ada menu. Tambahkan menu untuk mulai mencatat produksi."));
        Content = Ui.Page(Ui.Heading("Menu restoran", "Daftar menu, harga normal, dan berat per porsi.",
            Ui.Button("Tambah menu", () => host.ShowMenuEditor(), true)), list);
    }
}

internal sealed class RestaurantProfileView : UserControl
{
    internal const string Subtitle = "Atur informasi restoran yang tampil kepada pelanggan.";
    internal const double FormWidth = 720;
    internal const double DescriptionHeight = 88;
    internal const double MapHeight = 232;
    internal TextBox NameInput { get; }
    internal TextBox AddressInput { get; }
    internal TextBox PhoneInput { get; }
    internal TextBox DescriptionInput { get; }
    internal ProfileTimeField OpeningInput { get; }
    internal ProfileTimeField ClosingInput { get; }
    internal Button SaveButton { get; }
    internal Button BackLink { get; }
    internal Border FormCard { get; }
    internal Border MapCard { get; }
    internal Control? LocationMap { get; }
    internal FieldGrid NameRow { get; }
    internal FieldGrid TimeRow { get; }
    internal StackPanel Actions { get; }
    internal Dictionary<string, FieldFrame> Frames { get; } = new();
    internal Dictionary<string, TextBlock> FieldErrors { get; } = new();
    private readonly TextBlock formError = Ui.FieldError();
    private readonly MainWindow host;
    private readonly DemoSession session;

    // Validation keys from AccountService mapped to the frames that show them.
    private static readonly (string Error, string Frame)[] ErrorFrames =
    [
        ("restaurantName", "name"), ("address", "address"), ("phone", "phone"), ("description", "description"),
        ("openingTime", "openingHour"), ("closingTime", "closingHour")
    ];

    internal RestaurantProfileView(MainWindow host, DemoSession session)
    {
        this.host = host;
        this.session = session;
        Background = Ui.WorkspaceFill;
        Styles.Add(Ui.FocusStyles());
        CollectionView.AddTextLinkStyles(Styles);
        var restaurant = session.Restaurant;
        TextBox Line(string placeholder, string? value)
        {
            var input = Ui.Input(placeholder, value ?? "");
            input.TextWrapping = TextWrapping.NoWrap;
            return input;
        }
        NameInput = Line("Masukkan nama restoran", restaurant.Name);
        AddressInput = Line("Masukkan alamat restoran", restaurant.Address);
        PhoneInput = Line("Masukkan nomor telepon", restaurant.Phone);
        DescriptionInput = Ui.Input("Masukkan deskripsi restoran", restaurant.Description ?? "", true);
        OpeningInput = new ProfileTimeField("Jam buka") { Value = restaurant.OpeningTime };
        ClosingInput = new ProfileTimeField("Jam tutup") { Value = restaurant.ClosingTime };
        TextBlock Error(string key)
        {
            var error = Ui.FieldError();
            error.IsVisible = false;
            FieldErrors[key] = error;
            return error;
        }
        Control Field(string key, string errorKey, string label, TextBox input)
        {
            var frame = Ui.TextField(input);
            Frames[key] = frame;
            AutomationProperties.SetName(input, label);
            return Ui.Stack(Ui.FieldGap, Ui.Text(label, 14, bold: true), frame, Error(errorKey));
        }
        Control Time(string key, string errorKey, string label, ProfileTimeField input)
        {
            Frames[key + "Hour"] = input.HourFrame; Frames[key + "Minute"] = input.MinuteFrame;
            return Ui.Stack(Ui.FieldGap, Ui.Text(label, 14, bold: true), input, Error(errorKey));
        }
        var description = Field("description", "description", "Deskripsi", DescriptionInput);
        var descriptionFrame = Frames["description"];
        descriptionFrame.Face.Height = DescriptionHeight;
        DescriptionInput.VerticalContentAlignment = VerticalAlignment.Top;
        DescriptionInput.Padding = new Thickness(Ui.FieldInset);
        SaveButton = Ui.Button("Simpan profil", () => { }, true);
        SaveButton.Click += async (_, _) => await SaveAsync();
        BackLink = CollectionView.CreateTextLink("Kembali", host.ShowDashboard);
        BackLink.VerticalAlignment = VerticalAlignment.Center;
        NameRow = new FieldGrid(Field("name", "restaurantName", "Nama restoran", NameInput), Field("phone", "phone", "Telepon", PhoneInput));
        TimeRow = new FieldGrid(Time("opening", "openingTime", "Jam buka", OpeningInput), Time("closing", "closingTime", "Jam tutup", ClosingInput));
        var fields = Ui.Stack(Ui.Spacing.Related, NameRow, Field("address", "address", "Alamat", AddressInput), description, TimeRow);
        formError.IsVisible = false;
        Actions = Ui.Row(Ui.Spacing.Related, BackLink, SaveButton);
        Actions.HorizontalAlignment = HorizontalAlignment.Right;
        FormCard = Ui.FormCard(Ui.Stack(Ui.Spacing.Section, fields, formError, Actions));
        if (restaurant.Location is { } location) LocationMap = MapView.Create(location, [new(location, false)], MapHeight);
        MapCard = Ui.FormCard(Ui.Stack(12, Ui.Text("Lokasi di peta", 16, bold: true),
            LocationMap ?? Ui.Text(MapView.Unavailable, 14, Ui.Muted)));
        Content = Ui.CenteredPage(FormWidth, Ui.Heading("Profil restoran", Subtitle),
            Ui.Form(Ui.Stack(Ui.Spacing.Related, FormCard, MapCard), FormWidth));
    }

    private async Task SaveAsync()
    {
        if (!SaveButton.IsEnabled) return;
        SaveButton.IsEnabled = false;
        formError.IsVisible = false;
        ShowErrors(null);
        try
        {
            var profile = new Replate.Domain.Services.RestaurantProfile(NameInput.Text ?? "", AddressInput.Text ?? "", PhoneInput.Text ?? "",
                OpeningInput.Value, ClosingInput.Value, DescriptionInput.Text ?? "");
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
        foreach (var (key, frameKey) in ErrorFrames)
        {
            var message = errors?.GetValueOrDefault(key);
            var error = FieldErrors[key];
            var frame = Frames[frameKey];
            error.Text = message;
            error.IsVisible = message is not null;
            frame.Invalid = error.IsVisible;
            AutomationProperties.SetHelpText(frame.Input, message ?? "");
            if (error.IsVisible) firstInvalid ??= frame.Input;
        }
        firstInvalid?.Focus();
    }
}

// Profile time field
internal sealed class ProfileTimeField : StackPanel
{
    internal const double PartWidth = 104;
    internal ComboBox Hour { get; }
    internal ComboBox Minute { get; }
    internal FieldFrame HourFrame { get; }
    internal FieldFrame MinuteFrame { get; }
    internal TextBlock Separator { get; }
    internal ProfileTimeField(string label)
    {
        Orientation = Orientation.Horizontal; Spacing = Ui.FieldGap; HorizontalAlignment = HorizontalAlignment.Left;
        Hour = Part(24, $"{label}, jam");
        Minute = Part(60, $"{label}, menit");
        HourFrame = Ui.SelectField(Hour); MinuteFrame = Ui.SelectField(Minute);
        HourFrame.Width = MinuteFrame.Width = PartWidth;
        Separator = Ui.Text(":", 15, bold: true);
        Children.Add(HourFrame); Children.Add(Separator); Children.Add(MinuteFrame);
        Hour.SelectionChanged += (_, _) =>
        {
            if (Hour.SelectedIndex >= 0 && Minute.SelectedIndex < 0) Minute.SelectedIndex = 0;
        };
    }
    internal TimeOnly? Value
    {
        get => Hour.SelectedIndex >= 0 && Minute.SelectedIndex >= 0 ? new TimeOnly(Hour.SelectedIndex, Minute.SelectedIndex) : null;
        set { Hour.SelectedIndex = value?.Hour ?? -1; Minute.SelectedIndex = value?.Minute ?? -1; }
    }
    private static ComboBox Part(int count, string name)
    {
        var box = new ComboBox { ItemsSource = Enumerable.Range(0, count).Select(i => i.ToString("00")).ToList(), PlaceholderText = "--" };
        AutomationProperties.SetName(box, name);
        return box;
    }
}

internal sealed class RestaurantListCard : Border
{
    internal RestaurantListCard(string? photoPath, Control details, Control facts, Control actions)
    {
        var photo = new MenuPhotoView(photoPath, 76, 76, placeholderSize: 10);
        photo.VerticalAlignment = details.VerticalAlignment = facts.VerticalAlignment = actions.VerticalAlignment = VerticalAlignment.Center;
        var row = Ui.Columns("Auto,*,132,Auto", photo, details, facts, actions);
        row.ColumnSpacing = Ui.Spacing.Related;
        Child = row;
        Background = Brushes.White;
        CornerRadius = new CornerRadius(16);
        BorderBrush = Ui.CardBorder;
        BorderThickness = Ui.CardBorderThickness;
        Padding = new Thickness(18);
    }

    internal static StackPanel Price(string label, decimal price, Control status) =>
        Ui.Stack(6, Ui.Text(label, 12, Ui.Muted), Ui.Text(DemoSession.Rupiah(price), 18, Ui.Green, true), status);

    internal static Border Status(string label, bool positive) => new()
    {
        Background = positive ? Ui.Selection : Ui.Sidebar,
        CornerRadius = new CornerRadius(6),
        Padding = new Thickness(8, 4),
        HorizontalAlignment = HorizontalAlignment.Left,
        Child = Ui.Text(label, 12, positive ? Ui.Green : Ui.Muted, true)
    };

    internal static Button Action(string label, string item, Action action, IBrush? background = null, IBrush? foreground = null)
    {
        var button = Ui.Button(label, action);
        button.MinHeight = 36;
        button.Padding = new Thickness(10, 6);
        button.Background = background ?? Brushes.White;
        button.Foreground = foreground ?? Ui.Ink;
        button.BorderBrush = Ui.CardBorder;
        button.BorderThickness = Ui.CardBorderThickness;
        button.Classes.AddRange([Ui.CardActionClass, Ui.CompactClass,
            foreground == Ui.Danger ? Ui.CardActionDangerClass : background == Ui.Selection ? Ui.CardActionTonalClass : Ui.CardActionNeutralClass]);
        AutomationProperties.SetName(button, $"{label} {item}");
        return button;
    }

    internal static Button PrimaryAction(string label, string item, Action action)
    {
        var button = Action(label, item, action, Ui.Green, Ui.OnPrimary);
        button.BorderBrush = Brushes.Transparent;
        button.Classes.Remove(Ui.CardActionNeutralClass);
        button.Classes.Add(Ui.PrimaryClass);
        return button;
    }
}

internal static class MenuDeletionDialog
{
    internal static async Task Show(MainWindow host, DemoSession session, Menu menu)
    {
        var dialog = new Window
        {
            Title = "Hapus Menu", Width = 560, SizeToContent = SizeToContent.Height,
            CanResize = false, CanMinimize = false, CanMaximize = false, ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = host.Background, FontFamily = host.FontFamily
        };
        var error = Ui.Text("", 14, Avalonia.Media.Brush.Parse("#963E28"));
        error.IsVisible = false;
        var back = Ui.Button("Kembali", () => dialog.Close());
        var delete = Ui.Button("Hapus Menu", () => host.Run(() => session.DeleteMenu(menu),
            () => { dialog.Close(); host.ShowMenus(); }, "Menu berhasil dihapus.",
            message => { error.Text = message; error.IsVisible = true; }), true);
        delete.Background = Avalonia.Media.Brush.Parse("#B3362D");
        var buttons = Ui.Row(12, back, delete);
        buttons.HorizontalAlignment = HorizontalAlignment.Right;
        var body = Ui.Stack(20, Ui.Text("Hapus Menu", 24, bold: true),
            Ui.Text($"Apakah Anda yakin ingin menghapus menu \"{menu.Name}\"? Tindakan ini tidak dapat dibatalkan."), error, buttons);
        body.Margin = new Thickness(24);
        dialog.Content = body;
        dialog.Opened += (_, _) => back.Focus();
        dialog.AddHandler(Avalonia.Input.InputElement.KeyDownEvent, (_, e) =>
        {
            if (e.Key != Avalonia.Input.Key.Escape) return;
            e.Handled = true;
            dialog.Close();
        }, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        await dialog.ShowDialog(host);
    }
}

public sealed class MenuEditorView : UserControl
{
    private const double MenuFormWidth = 860;
    internal const double DescriptionHeight = 112;
    internal const double PhotoColumnWidth = 284;
    internal const double PhotoButtonHeight = 44;
    internal const double PhotoNoteSize = 13;
    internal const string PhotoNoteText = "Opsional. JPG, JPEG, atau PNG, maksimal 5 MB.";
    internal TextBox NameInput { get; }
    internal TextBox DescriptionInput { get; }
    internal ComboBox CategoryInput { get; }
    internal NumericUpDown PriceInput { get; }
    internal NumericUpDown WeightInput { get; }
    internal NumberStepper PriceStepper { get; }
    internal NumberStepper WeightStepper { get; }
    internal Button SaveButton { get; }
    internal Button CancelLink { get; }
    internal Border DetailsCard { get; }
    internal Border PhotoCard { get; }
    internal Grid NumberRow { get; }
    internal StackPanel Actions { get; }
    internal TextBlock PhotoNote { get; }
    internal Dictionary<string, FieldFrame> Frames { get; } = new();
    internal Dictionary<string, TextBlock> FieldErrors { get; } = new();
    private readonly DemoSession photoSession;
    private readonly Menu? originalMenu;
    private readonly ContentControl photoHost = new()
    {
        HorizontalContentAlignment = HorizontalAlignment.Stretch,
        VerticalContentAlignment = VerticalAlignment.Stretch
    };
    private MenuPhotoChange? photoChange;
    internal TextBlock PhotoError { get; } = Ui.FieldError();
    internal Button PickPhotoButton { get; }
    internal Button RemovePhotoButton { get; }
    internal MenuPhotoView PhotoPreview => (MenuPhotoView)photoHost.Content!;
    internal MenuEditorView(MainWindow host, DemoSession session, Menu? menu)
    {
        Background = Ui.WorkspaceFill;
        Styles.Add(Ui.FocusStyles());
        CollectionView.AddTextLinkStyles(Styles);
        Styles.Add(new Style(x => x.OfType<Button>().Class(Ui.CardActionClass).Class(":disabled").Template().OfType<ContentPresenter>().Name("PART_ContentPresenter"))
        {
            Setters =
            {
                new Setter(ContentPresenter.ForegroundProperty, new Avalonia.Data.TemplateBinding(TemplatedControl.ForegroundProperty)),
                new Setter(ContentPresenter.BorderBrushProperty, new Avalonia.Data.TemplateBinding(TemplatedControl.BorderBrushProperty))
            }
        });
        photoSession = session; originalMenu = menu;
        PhotoError.IsVisible = false;
        PickPhotoButton = Ui.Button("Pilih foto", async () =>
        {
            SaveButton!.IsEnabled = false;
            try
            {
                if (!host.StorageProvider.CanOpen) throw new InvalidOperationException("Pemilih file tidak tersedia pada perangkat ini.");
                var files = await host.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
                {
                    Title = "Pilih Foto Menu", AllowMultiple = false,
                    FileTypeFilter = new[] { new Avalonia.Platform.Storage.FilePickerFileType("Foto JPG dan PNG")
                    { Patterns = new[] { "*.jpg", "*.jpeg", "*.png" }, MimeTypes = new[] { "image/jpeg", "image/png" } } }
                });
                if (files.Count == 0) return;
                using var file = files[0];
                await using var stream = await file.OpenReadAsync();
                await SelectPhoto(stream, file.Name);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or NotSupportedException)
            { ShowPhotoError(e.Message); }
            finally { SaveButton!.IsEnabled = true; }
        });
        RemovePhotoButton = Ui.Button("Hapus foto", RemovePhoto);
        PhotoAction(PickPhotoButton, Ui.Selection, Ui.Green, Brushes.Transparent, Ui.CardActionTonalClass);
        PhotoAction(RemovePhotoButton, Ui.Surface, Ui.Ink, Ui.CardBorder, Ui.CardActionNeutralClass);
        RemovePhotoButton.PropertyChanged += (_, e) => { if (e.Property == IsEnabledProperty) SyncRemovePhoto(); };
        UpdatePhotoPreview();
        SyncRemovePhoto();
        PhotoNote = Ui.Text(PhotoNoteText, PhotoNoteSize, Ui.Muted);
        var photoControls = Ui.Stack(Ui.Spacing.Related, PhotoNote, Ui.Row(Ui.FieldGap, PickPhotoButton, RemovePhotoButton), PhotoError);
        var photoField = new Grid
        {
            RowDefinitions = RowDefinitions.Parse("Auto,*,Auto"), RowSpacing = Ui.Spacing.Related,
            Children = { Ui.Text("Foto menu", 18, bold: true), photoHost, photoControls }
        };
        Grid.SetRow(photoHost, 1);
        Grid.SetRow(photoControls, 2);
        NameInput = Ui.Input("Masukkan nama menu", menu?.Name ?? "");
        NameInput.TextWrapping = TextWrapping.NoWrap;
        DescriptionInput = Ui.Input("Masukkan deskripsi menu", menu?.Description ?? "", true);
        CategoryInput = new ComboBox { ItemsSource = MenuCategories.Names, SelectedItem = menu?.Category ?? "Makanan utama" };
        var category = CategoryInput;
        PriceInput = Ui.Number("Masukkan harga", menu?.NormalPrice ?? 20000, 10000000, 1000);
        WeightInput = Ui.Number("Masukkan berat per porsi", menu?.WeightPerPortion ?? .3m, 100, .05m);
        PriceStepper = new NumberStepper(PriceInput, "Harga normal (Rp)");
        WeightStepper = new NumberStepper(WeightInput, "Berat per porsi (kg)");
        Frames["name"] = Ui.TextField(NameInput); Frames["category"] = Ui.SelectField(category);
        Frames["price"] = PriceStepper.Frame; Frames["weight"] = WeightStepper.Frame;
        Frames["description"] = Ui.TextField(DescriptionInput);
        Frames["description"].Face.Height = DescriptionHeight;
        DescriptionInput.VerticalContentAlignment = VerticalAlignment.Top;
        DescriptionInput.Padding = new Thickness(Ui.FieldInset);
        var inputs = new Dictionary<string, Control>
        {
            ["name"] = NameInput, ["category"] = category, ["price"] = PriceInput,
            ["weight"] = WeightInput, ["description"] = DescriptionInput
        };
        var shells = new Dictionary<string, Control>
        {
            ["name"] = Frames["name"], ["category"] = Frames["category"], ["price"] = PriceStepper,
            ["weight"] = WeightStepper, ["description"] = Frames["description"]
        };
        var attempted = false;
        Control Field(string key, string label)
        {
            var error = Ui.FieldError(); error.IsVisible = false;
            FieldErrors[key] = error;
            Avalonia.Automation.AutomationProperties.SetName(inputs[key], label);
            return Ui.Stack(Ui.FieldGap, Ui.Text(label, 14, bold: true), shells[key], error);
        }
        decimal? ReadNumber(NumericUpDown input) => decimal.TryParse(input.Text, input.ParsingNumberStyle,
            input.NumberFormat, out var value) ? value : null;
        Dictionary<string, string> Errors()
        {
            var errors = session.ValidateMenu(menu, NameInput.Text ?? "", DescriptionInput.Text ?? "",
                ReadNumber(PriceInput), ReadNumber(WeightInput));
            void CheckNumber(string key, NumericUpDown input, string label)
            {
                if (string.IsNullOrWhiteSpace(input.Text)) return;
                var value = ReadNumber(input);
                if (value is null) errors[key] = $"{label} harus berupa angka yang valid.";
                else if (value < input.Minimum || value > input.Maximum)
                    errors[key] = $"{label} harus antara {input.Minimum} dan {input.Maximum}.";
            }
            CheckNumber("price", PriceInput, "Harga");
            CheckNumber("weight", WeightInput, "Berat per porsi");
            return errors;
        }
        void Display(string key, Dictionary<string, string> errors)
        {
            var invalid = errors.TryGetValue(key, out var message);
            FieldErrors[key].Text = message ?? "";
            FieldErrors[key].IsVisible = invalid;
            Frames[key].Invalid = invalid;
            Avalonia.Automation.AutomationProperties.SetHelpText(inputs[key], message ?? "");
        }
        SaveButton = Ui.Button("Simpan menu", () =>
        {
            attempted = true;
            var errors = Errors();
            foreach (var key in inputs.Keys) Display(key, errors);
            if (errors.Count > 0)
            {
                var first = inputs.First(p => errors.ContainsKey(p.Key)).Value;
                first.BringIntoView();
                if (first is NumericUpDown number) number.GetVisualDescendants().OfType<TextBox>().FirstOrDefault()?.Focus(); else first.Focus();
                return;
            }
            if (PhotoError.IsVisible)
            {
                PickPhotoButton.BringIntoView(); PickPhotoButton.Focus(); return;
            }
            host.Run(() => session.SaveMenu(menu, NameInput.Text ?? "", DescriptionInput.Text ?? "",
                category.SelectedItem?.ToString() ?? "Makanan utama", ReadNumber(PriceInput)!.Value, ReadNumber(WeightInput)!.Value, photoChange),
                host.ShowMenus, "Menu berhasil disimpan.");
        }, true);
        NumberRow = Ui.Columns("*,*", Field("price", "Harga normal (Rp)"), Field("weight", "Berat per porsi (kg)"));
        NumberRow.ColumnSpacing = Ui.Spacing.Related;
        var details = Ui.Stack(Ui.Spacing.Related, Field("name", "Nama menu"), Field("category", "Kategori"), NumberRow,
            Field("description", "Deskripsi"));
        CancelLink = CollectionView.CreateTextLink("Batal", host.ShowMenus);
        CancelLink.VerticalAlignment = VerticalAlignment.Center;
        Actions = Ui.Row(Ui.Spacing.Related, CancelLink, SaveButton);
        Actions.HorizontalAlignment = HorizontalAlignment.Right;
        DetailsCard = Ui.FormCard(details);
        PhotoCard = Ui.FormCard(photoField);
        var side = new Grid
        {
            RowDefinitions = RowDefinitions.Parse("*,Auto"), RowSpacing = Ui.Spacing.Related,
            Children = { PhotoCard, Actions }
        };
        Grid.SetRow(Actions, 1);
        var form = Ui.Columns($"*,{PhotoColumnWidth}", DetailsCard, side);
        form.ColumnSpacing = Ui.Spacing.Related;
        foreach (var pair in inputs)
            pair.Value.PropertyChanged += (_, e) =>
            {
                if (attempted && (e.Property == TextBox.TextProperty || e.Property == NumericUpDown.TextProperty || e.Property == NumericUpDown.ValueProperty))
                    Display(pair.Key, Errors());
            };
        Content = Ui.CenteredPage(MenuFormWidth, Ui.Heading(menu is null ? "Tambah menu" : "Ubah menu",
            menu?.Name ?? "Siapkan menu untuk pencatatan produksi dan penawaran surplus."), Ui.Form(form, MenuFormWidth));
    }
    internal async Task SelectPhoto(Stream stream, string name)
    {
        try
        {
            var selected = await MenuPhotoChange.Read(stream, name);
            photoChange = selected;
            PhotoError.Text = ""; PhotoError.IsVisible = false;
            UpdatePhotoPreview();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or NotSupportedException)
        { ShowPhotoError(e.Message); }
    }
    internal void RemovePhoto()
    {
        photoChange = MenuPhotoChange.Remove;
        PhotoError.Text = ""; PhotoError.IsVisible = false;
        UpdatePhotoPreview();
    }
    private void ShowPhotoError(string message)
    {
        PhotoError.Text = message;
        PhotoError.IsVisible = true;
        RemovePhotoButton.IsEnabled = true;
    }
    private void UpdatePhotoPreview()
    {
        var path = photoChange is null ? photoSession.PhotoPath(originalMenu?.ImageUrl) : null;
        RemovePhotoButton.IsEnabled = path is not null || photoChange?.Data is not null;
        photoHost.Content = new MenuPhotoView(path, 242, 156, photoChange?.Data)
        { Width = double.NaN, Height = double.NaN, MinHeight = 156, MaxHeight = 260 };
    }
    private static void PhotoAction(Button button, IBrush background, IBrush foreground, IBrush border, string role)
    {
        button.Height = PhotoButtonHeight;
        button.Padding = new Thickness(Ui.FieldInset, 0);
        button.VerticalContentAlignment = VerticalAlignment.Center;
        button.Background = background; button.Foreground = foreground;
        button.BorderBrush = border; button.BorderThickness = Ui.CardBorderThickness;
        button.Classes.AddRange([Ui.CardActionClass, Ui.CompactClass, role]);
    }
    private void SyncRemovePhoto()
    {
        var enabled = RemovePhotoButton.IsEnabled;
        RemovePhotoButton.Background = enabled ? Ui.Surface : Ui.Sidebar;
        RemovePhotoButton.Foreground = enabled ? Ui.Ink : Ui.MutedFaint;
        RemovePhotoButton.BorderBrush = enabled ? Ui.CardBorder : Ui.Sidebar;
    }
}
public sealed class ListingsView : UserControl
{
    internal ListingsView(MainWindow host, DemoSession session)
    {
        Background = Ui.WorkspaceFill;
        Styles.Add(Ui.FocusStyles());
        var list = Ui.Stack(Ui.Spacing.Related);
        var now = DateTime.UtcNow;
        foreach (var listing in session.RestaurantListings.OrderByDescending(l => l.CreatedAt))
        {
            var ended = listing.PickupEnd <= now || listing.Status is ListingStatus.Cancelled or ListingStatus.Expired;
            var status = listing.Status == ListingStatus.Cancelled ? "Dibatalkan" : ended ? "Berakhir" :
                listing.Quantity == 0 ? "Habis" : !listing.Menu.IsActive ? "Menu nonaktif" : listing.PickupStart > now ? "Terjadwal" : "Tersedia";
            var name = listing.Menu.Name;
            var buttons = Ui.Row(8);
            buttons.MinWidth = 126;
            if (!ended)
            {
                buttons.Children.Add(RestaurantListCard.Action("Ubah", name, () => host.ShowListingEditor(listing), Ui.Selection, Ui.Green));
                buttons.Children.Add(RestaurantListCard.Action("Tutup", name, () => host.Run(() => session.CancelListing(listing), host.ShowListings)));
            }
            else if (listing.Status == ListingStatus.Cancelled)
            {
                if (DemoSession.ReopenBlocker(listing, now) is { } blocker)
                {
                    var note = Ui.Text(blocker, 12, Ui.Muted);
                    note.MaxWidth = 150;
                    buttons.Children.Add(note);
                }
                else
                    buttons.Children.Add(RestaurantListCard.PrimaryAction("Buka kembali", name,
                        () => host.Run(() => session.ReopenListing(listing), host.ShowListings)));
            }
            var start = listing.PickupStart.ToLocalTime();
            var end = listing.PickupEnd.ToLocalTime();
            var endTime = start.Date == end.Date ? $"{end:HH:mm}" : $"{end:dd MMM, HH:mm}";
            var details = Ui.Stack(4, Ui.Text(name, 18, bold: true),
                Ui.Text($"Ambil {start:dd MMM, HH:mm}–{endTime}", 13, Ui.Muted),
                Ui.Text($"{listing.Quantity} porsi tersisa", 13, Ui.Muted));
            var facts = RestaurantListCard.Price("Harga surplus", listing.RescuePrice,
                RestaurantListCard.Status(status, status == "Tersedia"));
            list.Children.Add(new RestaurantListCard(session.PhotoPath(listing.Menu.ImageUrl), details, facts, buttons));
        }
        if (list.Children.Count == 0) list.Children.Add(Ui.Text("Belum ada penawaran. Catat produksi, lalu tawarkan surplus yang tersedia."));
        Content = Ui.Page(Ui.Heading("Penawaran surplus", "Stok yang ditawarkan kepada pelanggan, terpisah dari stok yang sudah dipesan.",
            Ui.Button("Buat penawaran", () => host.ShowListingEditor(), true)), list);
    }
}

public sealed class ListingEditorView : UserControl
{
    internal const double FormWidth = 720;
    internal const double NoteSize = 13;
    internal const string NoteText = "Stok tidak boleh melebihi surplus dalam catatan produksi. Jadwal pesanan aktif tidak dapat diubah.";
    internal ComboBox MenuInput { get; private set; } = null!;
    internal NumericUpDown PriceInput { get; private set; } = null!;
    internal NumericUpDown StockInput { get; private set; } = null!;
    internal NumberStepper PriceStepper { get; private set; } = null!;
    internal NumberStepper StockStepper { get; private set; } = null!;
    internal DateField DateInput { get; private set; } = null!;
    internal ProfileTimeField StartInput { get; private set; } = null!;
    internal ProfileTimeField EndInput { get; private set; } = null!;
    internal Button SaveButton { get; private set; } = null!;
    internal Button CancelLink { get; private set; } = null!;
    internal Border FormCard { get; private set; } = null!;
    internal FieldGrid NumberRow { get; private set; } = null!;
    internal FieldGrid TimeRow { get; private set; } = null!;
    internal TextBlock Note { get; private set; } = null!;
    internal StackPanel Actions { get; private set; } = null!;
    internal Dictionary<string, FieldFrame> Frames { get; } = new();
    internal ListingEditorView(MainWindow host, DemoSession session, SurplusListing? listing)
    {
        Background = Ui.WorkspaceFill;
        Styles.Add(Ui.FocusStyles());
        CollectionView.AddTextLinkStyles(Styles);
        var menus = session.Restaurant.Menus.Where(m => m.IsActive || m == listing?.Menu).ToList();
        if (menus.Count == 0)
        {
            Content = Ui.Page(Ui.Heading("Buat penawaran", "Tambahkan menu aktif terlebih dahulu."), Ui.Button("Tambah menu", () => host.ShowMenuEditor(), true)); return;
        }
        MenuInput = new ComboBox { ItemsSource = menus, SelectedItem = listing?.Menu ?? menus[0],
            ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<Menu>((m, _) => new TextBlock
            { Text = m?.Name ?? "", VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis }) };
        MenuInput.IsEnabled = listing is null;
        var menu = MenuInput;
        PriceInput = Ui.Number("Masukkan harga penawaran", listing?.RescuePrice ?? menus[0].NormalPrice / 2, 10000000, 1000);
        StockInput = Ui.Number("Masukkan stok", listing?.Quantity ?? 1, 100000); StockInput.Minimum = 1;
        var (price, stock) = (PriceInput, StockInput);
        PriceStepper = new NumberStepper(price, "Harga penawaran (Rp)");
        StockStepper = new NumberStepper(stock, "Stok tersedia (porsi)");
        var start = listing?.PickupStart.ToLocalTime() ?? DateTime.Now.AddMinutes(-5);
        var end = listing?.PickupEnd.ToLocalTime() ?? DateTime.Now.AddHours(2);
        if (end.Date != start.Date) end = start.Date.AddDays(1).AddSeconds(-1);
        DateInput = new DateField(DateTime.MaxValue.Date) { SelectedDate = new DateTimeOffset(start.Date) };
        StartInput = new ProfileTimeField("Mulai") { Value = new TimeOnly(start.Hour, start.Minute) };
        EndInput = new ProfileTimeField("Selesai") { Value = new TimeOnly(end.Hour, end.Minute) };
        var (date, from, until) = (DateInput, StartInput, EndInput);
        Frames["menu"] = Ui.SelectField(menu); Frames["price"] = PriceStepper.Frame; Frames["stock"] = StockStepper.Frame; Frames["date"] = DateInput.Frame;
        Frames["startHour"] = from.HourFrame; Frames["startMinute"] = from.MinuteFrame;
        Frames["endHour"] = until.HourFrame; Frames["endMinute"] = until.MinuteFrame;
        SaveButton = Ui.Button("Simpan penawaran", () => host.Run(() =>
        {
            if (date.SelectedDate is null || from.Value is null || until.Value is null)
                throw new InvalidOperationException("Lengkapi tanggal dan waktu pengambilan.");
            session.SaveListing(listing, (Menu)menu.SelectedItem!, price.Value ?? 0, (int)(stock.Value ?? 0),
                date.SelectedDate.Value.Date + from.Value.Value.ToTimeSpan(), date.SelectedDate.Value.Date + until.Value.Value.ToTimeSpan());
        }, host.ShowListings), true);
        CancelLink = CollectionView.CreateTextLink("Batal", host.ShowListings);
        CancelLink.VerticalAlignment = VerticalAlignment.Center;
        Control Field(string label, Control shell, Control named)
        {
            AutomationProperties.SetName(named, label);
            return Ui.Stack(Ui.FieldGap, Ui.Text(label, 14, bold: true), shell);
        }
        NumberRow = new FieldGrid(Field("Harga penawaran (Rp)", PriceStepper, price), Field("Stok tersedia (porsi)", StockStepper, stock));
        TimeRow = new FieldGrid(Ui.Stack(Ui.FieldGap, Ui.Text("Mulai", 14, bold: true), from), Ui.Stack(Ui.FieldGap, Ui.Text("Selesai", 14, bold: true), until));
        Note = Ui.Text(NoteText, NoteSize, Ui.Muted);
        var fields = Ui.Stack(Ui.Spacing.Related, Field("Menu", Frames["menu"], menu), NumberRow,
            Field("Tanggal pengambilan", DateInput, DateInput.Face), TimeRow, Note);
        Actions = Ui.Row(Ui.Spacing.Related, CancelLink, SaveButton);
        Actions.HorizontalAlignment = HorizontalAlignment.Right;
        FormCard = Ui.FormCard(Ui.Stack(Ui.Spacing.Section, fields, Actions));
        Content = Ui.CenteredPage(FormWidth,
            Ui.Heading(listing is null ? "Buat penawaran" : "Ubah penawaran", "Harga khusus dan jadwal pengambilan makanan surplus."),
            Ui.Form(FormCard, FormWidth));
    }
}

public sealed class ProductionView : UserControl
{
    internal const string RescuedLabel = "Terselamatkan";
    internal const string RescuedNoteText = "Bertambah setelah kode pengambilan diverifikasi.";
    internal ComboBox MenuInput { get; private set; } = null!;
    internal DateField DateInput { get; private set; } = null!;
    internal NumericUpDown ProducedInput { get; private set; } = null!;
    internal NumericUpDown SoldInput { get; private set; } = null!;
    internal NumberStepper ProducedStepper { get; private set; } = null!;
    internal NumberStepper SoldStepper { get; private set; } = null!;
    internal Button SaveButton { get; private set; } = null!;
    internal Border FormCard { get; private set; } = null!;
    internal FieldGrid Fields { get; private set; } = null!;
    internal Avalonia.Controls.Documents.Run RescuedCount { get; private set; } = null!;
    internal TextBlock RescuedNote { get; private set; } = null!;
    internal Dictionary<string, FieldFrame> Frames { get; } = new();
    internal Dictionary<string, TextBlock> FieldErrors { get; } = new();
    internal ProductionView(MainWindow host, DemoSession session)
    {
        Background = Ui.WorkspaceFill;
        Styles.Add(Ui.FocusStyles());
        var menus = session.RestaurantMenus.ToList();
        if (menus.Count == 0)
        {
            Content = Ui.Page(Ui.Heading("Catatan produksi", "Tambahkan menu sebelum mencatat produksi."), Ui.Button("Tambah menu", () => host.ShowMenuEditor(), true)); return;
        }
        DateInput = new DateField(new DateTime(DateTime.Today.Year, 12, 31)) { SelectedDate = DateTimeOffset.Now };
        MenuInput = new ComboBox { ItemsSource = menus, SelectedIndex = 0, PlaceholderText = "Pilih menu",
            ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<Menu>((m, _) => new TextBlock
            { Text = m?.Name ?? "", VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis }) };
        ProducedInput = Ui.Number("Masukkan jumlah produksi"); SoldInput = Ui.Number("Masukkan jumlah terjual");
        ProducedStepper = new NumberStepper(ProducedInput, "Diproduksi (porsi)");
        SoldStepper = new NumberStepper(SoldInput, "Terjual reguler (porsi)");
        Frames["menu"] = Ui.SelectField(MenuInput); Frames["date"] = DateInput.Frame;
        Frames["produced"] = ProducedStepper.Frame; Frames["sold"] = SoldStepper.Frame;
        RescuedCount = new Avalonia.Controls.Documents.Run("0") { FontSize = 28, FontWeight = FontWeight.Bold, Foreground = Ui.Green };
        var table = Ui.Stack(0);
        var inputs = new Dictionary<string, Control>
        { ["menu"] = MenuInput, ["date"] = DateInput, ["produced"] = ProducedInput, ["sold"] = SoldInput };
        var shells = new Dictionary<string, Control>
        { ["menu"] = Frames["menu"], ["date"] = DateInput, ["produced"] = ProducedStepper, ["sold"] = SoldStepper };
        var attempted = false;
        var refreshing = false;
        DateOnly? SelectedDate() => DateInput.SelectedDate is { } date ? DateOnly.FromDateTime(date.Date) : null;
        int? ReadNumber(NumericUpDown input) => int.TryParse(input.Text, input.ParsingNumberStyle, input.NumberFormat, out var value) ? value : null;
        Control Target(Control input) => input is DateField date ? date.Face : input;
        Control Field(string key, string label)
        {
            var error = Ui.FieldError(); error.IsVisible = false;
            FieldErrors[key] = error;
            Avalonia.Automation.AutomationProperties.SetName(Target(inputs[key]), label);
            return Ui.Stack(Ui.FieldGap, Ui.Text(label, 14, bold: true), shells[key], error);
        }
        Dictionary<string, string> Errors()
        {
            var errors = session.ValidateProduction(MenuInput.SelectedItem as Menu, SelectedDate(), ReadNumber(ProducedInput), ReadNumber(SoldInput));
            void CheckNumber(string key, NumericUpDown input, string label)
            {
                if (string.IsNullOrWhiteSpace(input.Text)) return;
                var value = ReadNumber(input);
                if (value is null) errors[key] = $"{label} harus berupa bilangan bulat yang valid.";
                else if (value > input.Maximum) errors[key] = $"{label} maksimal {input.Maximum} porsi.";
            }
            CheckNumber("produced", ProducedInput, "Produksi");
            CheckNumber("sold", SoldInput, "Penjualan reguler");
            return errors;
        }
        void Display(Dictionary<string, string> errors)
        {
            foreach (var pair in inputs)
            {
                var invalid = errors.TryGetValue(pair.Key, out var message);
                FieldErrors[pair.Key].Text = message ?? "";
                FieldErrors[pair.Key].IsVisible = invalid;
                Frames[pair.Key].Invalid = invalid;
                Avalonia.Automation.AutomationProperties.SetHelpText(Target(pair.Value), message ?? "");
            }
        }
        void Revalidate() { if (attempted && !refreshing) Display(Errors()); }
        void Refresh()
        {
            refreshing = true;
            var selected = MenuInput.SelectedItem as Menu;
            var selectedDate = SelectedDate();
            if (selected is not null && selectedDate is not null)
            {
                var record = selected.ProductionRecords.SingleOrDefault(r => r.RecordDate == selectedDate);
                ProducedInput.Value = record?.ProducedQuantity ?? 0; SoldInput.Value = record?.RegularSoldQuantity ?? 0;
                RescuedCount.Text = (record?.RescuedQuantity ?? 0).ToString();
                RescuedNote.Text = record is not null && !session.IsDashboardProduction(record)
                    ? RescuedNoteText + " Catatan ini masih di komputer. Simpan catatan untuk mengirimnya ke Supabase."
                    : RescuedNoteText;
            }
            table.Children.Clear();
            table.Children.Add(Ui.TableRow("1.6*,*,*,*,*", true, Ui.Text("Menu", 13, Ui.Muted), Ui.Text("Produksi", 13, Ui.Muted),
                Ui.Text("Terjual reguler", 13, Ui.Muted), Ui.Text("Surplus", 13, Ui.Muted), Ui.Text("Terselamatkan", 13, Ui.Muted)));
            if (selectedDate is not null)
                foreach (var m in menus)
                {
                    var r = m.ProductionRecords.SingleOrDefault(r => r.RecordDate == selectedDate);
                    table.Children.Add(Ui.TableRow("1.6*,*,*,*,*", false, Ui.Text(m.Name, 14, bold: true),
                        Ui.Text((r?.ProducedQuantity ?? 0).ToString()), Ui.Text((r?.RegularSoldQuantity ?? 0).ToString()),
                        Ui.Text((r?.CalculateSurplus() ?? 0).ToString()), Ui.Text((r?.RescuedQuantity ?? 0).ToString())));
                }
            refreshing = false;
            Revalidate();
        }
        SaveButton = Ui.Button("Simpan catatan", () =>
        {
            attempted = true;
            var errors = Errors(); Display(errors);
            if (errors.Count > 0)
            {
                var first = inputs.First(p => errors.ContainsKey(p.Key)).Value;
                first.BringIntoView();
                if (first is NumericUpDown number) number.GetVisualDescendants().OfType<TextBox>().FirstOrDefault()?.Focus(); else Target(first).Focus();
                return;
            }
            host.Run(() => session.SaveProduction((Menu)MenuInput.SelectedItem!, SelectedDate()!.Value,
                ReadNumber(ProducedInput)!.Value, ReadNumber(SoldInput)!.Value), Refresh);
        }, true);
        Fields = new FieldGrid(Field("menu", "Menu"), Field("date", "Tanggal produksi"),
            Field("produced", "Diproduksi (porsi)"), Field("sold", "Terjual reguler (porsi)"));
        var amount = new TextBlock { Inlines = new Avalonia.Controls.Documents.InlineCollection
            { RescuedCount, new Avalonia.Controls.Documents.Run(" porsi") { FontSize = 14, Foreground = Ui.Muted } } };
        RescuedNote = Ui.Text(RescuedNoteText, 14, Ui.Muted);
        var summary = Ui.Stack(4, Ui.Text(RescuedLabel, 12, Ui.Muted), amount, RescuedNote);
        SaveButton.VerticalAlignment = VerticalAlignment.Bottom;
        var footer = Ui.Columns("*,Auto", summary, SaveButton);
        footer.ColumnSpacing = Ui.Spacing.Related;
        FormCard = Ui.FormCard(Ui.Stack(Ui.Spacing.Section, Fields, Ui.Divider(), footer));
        DateInput.SelectedDateChanged += (_, _) => Refresh(); MenuInput.SelectionChanged += (_, _) => Refresh();
        foreach (var number in new[] { ProducedInput, SoldInput })
            number.PropertyChanged += (_, e) =>
            {
                if (e.Property == NumericUpDown.TextProperty || e.Property == NumericUpDown.ValueProperty) Revalidate();
            };
        Refresh();
        Content = Ui.Page(Ui.Heading("Catatan produksi", "Catat produksi dan penjualan reguler untuk menghitung surplus per menu."), Ui.Form(FormCard),
            Ui.Divider(), Ui.Text("Ringkasan tanggal terpilih", 20, bold: true), table);
    }
}
