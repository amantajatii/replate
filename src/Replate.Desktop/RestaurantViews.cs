using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Replate.Domain.Entities;
using Replate.Domain.Enums;
using Menu = Replate.Domain.Entities.Menu;

namespace Replate.Desktop;

public sealed class MenusView : UserControl
{
    internal MenusView(MainWindow host, DemoSession session)
    {
        var list = Ui.Stack(0);
        foreach (var menu in session.Restaurant.Menus)
            list.Children.Add(Ui.TableRow("*,120,90,300", false,
                Ui.Stack(6, Ui.Text(menu.Name, 18, bold: true), Ui.Text(menu.Category, 13, Ui.Muted), Ui.Text(menu.Description, 13, Ui.Muted)),
                Ui.Text(DemoSession.Rupiah(menu.NormalPrice), 14), Ui.Text(menu.IsActive ? "Aktif" : "Nonaktif", 13),
                Ui.Row(8, Ui.Button("Ubah", () => host.ShowMenuEditor(menu)),
                    Ui.Button(menu.IsActive ? "Nonaktifkan" : "Aktifkan", () => host.Run(() => session.ToggleMenu(menu), host.ShowMenus)), Ui.Button("Hapus", async () => await MenuDeletionDialog.Show(host, session, menu)))));
        if (list.Children.Count == 0) list.Children.Add(Ui.Text("Belum ada menu. Tambahkan menu untuk mulai mencatat produksi."));
        Content = Ui.Page(Ui.Heading("Menu restoran", "Daftar menu, harga normal, dan berat per porsi.",
            Ui.Button("Tambah menu", () => host.ShowMenuEditor(), true)), list);
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
    internal TextBox NameInput { get; }
    internal TextBox DescriptionInput { get; }
    internal NumericUpDown PriceInput { get; }
    internal NumericUpDown WeightInput { get; }
    internal Button SaveButton { get; }
    internal Dictionary<string, TextBlock> FieldErrors { get; } = new();
    private readonly DemoSession photoSession;
    private readonly Menu? originalMenu;
    private readonly ContentControl photoHost = new();
    private MenuPhotoChange? photoChange;
    internal TextBlock PhotoError { get; } = Ui.FieldError();
    internal Button PickPhotoButton { get; }
    internal Button RemovePhotoButton { get; }
    internal MenuPhotoView PhotoPreview => (MenuPhotoView)photoHost.Content!;
    internal MenuEditorView(MainWindow host, DemoSession session, Menu? menu)
    {
        photoSession = session; originalMenu = menu;
        PhotoError.IsVisible = false;
        UpdatePhotoPreview();
        PickPhotoButton = Ui.Button("Pilih Foto", async () =>
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
        RemovePhotoButton = Ui.Button("Hapus Foto", RemovePhoto);
        var photoField = Ui.Stack(8, Ui.Text("Foto menu", 14, bold: true), photoHost,
            Ui.SupportingText("Opsional. JPG, JPEG, atau PNG, maksimal 5 MB."),
            Ui.Row(12, PickPhotoButton, RemovePhotoButton), PhotoError);
        NameInput = Ui.Input(menu?.Name ?? "");
        DescriptionInput = Ui.Input(menu?.Description ?? "", true);
        var category = new ComboBox { ItemsSource = new[] { "Makanan utama", "Roti & kue", "Vegetarian" }, SelectedItem = menu?.Category ?? "Makanan utama", MinHeight = 42 };
        PriceInput = Ui.Number(menu?.NormalPrice ?? 20000, 10000000, 1000);
        WeightInput = Ui.Number(menu?.WeightPerPortion ?? .3m, 100, .05m);
        var inputs = new Dictionary<string, Control>
        {
            ["name"] = NameInput, ["description"] = DescriptionInput, ["category"] = category,
            ["price"] = PriceInput, ["weight"] = WeightInput
        };
        var outlines = new Dictionary<string, Border>();
        var red = Avalonia.Media.Brush.Parse("#B3261E");
        var attempted = false;
        Control Field(string key, string label)
        {
            inputs[key].HorizontalAlignment = inputs[key] is DatePicker or TimePicker ? HorizontalAlignment.Left : HorizontalAlignment.Stretch;
            var error = Ui.FieldError(); error.IsVisible = false;
            FieldErrors[key] = error;
            var outline = new Border { Child = inputs[key], CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(1), BorderBrush = Avalonia.Media.Brushes.Transparent };
            outlines[key] = outline;
            Avalonia.Automation.AutomationProperties.SetName(inputs[key], label);
            return Ui.Stack(8, Ui.Text(label, 14, bold: true), outline, error);
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
            outlines[key].BorderBrush = invalid ? red : Avalonia.Media.Brushes.Transparent;
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
        var form = Ui.Stack(20, Field("name", "Nama menu"), Field("description", "Deskripsi"), Field("category", "Kategori"),
            Ui.FormPair(Field("price", "Harga normal (Rp)"), Field("weight", "Berat per porsi (kg)")), photoField,
            Ui.Row(12, SaveButton, Ui.Button("Batal", host.ShowMenus)));
        foreach (var pair in inputs)
            pair.Value.PropertyChanged += (_, e) =>
            {
                if (attempted && (e.Property == TextBox.TextProperty || e.Property == NumericUpDown.TextProperty || e.Property == NumericUpDown.ValueProperty))
                    Display(pair.Key, Errors());
            };
        Content = Ui.Page(Ui.Heading(menu is null ? "Tambah menu" : "Ubah menu", "Menu dipakai untuk pencatatan produksi dan penawaran surplus."), Ui.Form(form));
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
    { PhotoError.Text = message; PhotoError.IsVisible = true; }
    private void UpdatePhotoPreview()
    {
        var path = photoChange is null ? photoSession.PhotoPath(originalMenu?.ImageUrl) : null;
        photoHost.Content = new MenuPhotoView(path, 240, 140, photoChange?.Data)
        { HorizontalAlignment = HorizontalAlignment.Left };
    }
}
public sealed class ListingsView : UserControl
{
    internal ListingsView(MainWindow host, DemoSession session)
    {
        var list = Ui.Stack(0);
        foreach (var listing in session.Listings.OrderByDescending(l => l.CreatedAt))
        {
            var ended = listing.PickupEnd <= DateTime.UtcNow || listing.Status is ListingStatus.Cancelled or ListingStatus.Expired;
            var status = listing.Status == ListingStatus.Cancelled ? "Dibatalkan" : listing.PickupEnd <= DateTime.UtcNow ? "Berakhir" :
                listing.Quantity == 0 ? "Habis" : !listing.Menu.IsActive ? "Menu nonaktif" : listing.PickupStart > DateTime.UtcNow ? "Terjadwal" : "Tersedia";
            var buttons = Ui.Row(8);
            if (!ended)
            {
                buttons.Children.Add(Ui.Button("Ubah", () => host.ShowListingEditor(listing)));
                buttons.Children.Add(Ui.Button("Tutup", () => host.Run(() => session.CancelListing(listing), host.ShowListings)));
            }
            list.Children.Add(Ui.TableRow("*,130,100,180", false,
                Ui.Stack(6, Ui.Text(listing.Menu.Name, 18, bold: true),
                    Ui.Text($"{listing.PickupStart.ToLocalTime():dd MMM, HH:mm}–{listing.PickupEnd.ToLocalTime():HH:mm} · {status}", 13, Ui.Muted)),
                Ui.Text(DemoSession.Rupiah(listing.RescuePrice), 14), Ui.Text($"{listing.Quantity} porsi", 14), buttons));
        }
        if (list.Children.Count == 0) list.Children.Add(Ui.Text("Belum ada penawaran. Catat produksi, lalu tawarkan surplus yang tersedia."));
        Content = Ui.Page(Ui.Heading("Penawaran surplus", "Stok yang ditawarkan kepada pelanggan, terpisah dari stok yang sudah dipesan.",
            Ui.Button("Buat penawaran", () => host.ShowListingEditor(), true)), list);
    }
}

public sealed class ListingEditorView : UserControl
{
    internal ListingEditorView(MainWindow host, DemoSession session, SurplusListing? listing)
    {
        var menus = session.Restaurant.Menus.Where(m => m.IsActive || m == listing?.Menu).ToList();
        if (menus.Count == 0)
        {
            Content = Ui.Page(Ui.Heading("Buat penawaran", "Tambahkan menu aktif terlebih dahulu."), Ui.Button("Tambah menu", () => host.ShowMenuEditor(), true)); return;
        }
        var menu = new ComboBox { ItemsSource = menus, SelectedItem = listing?.Menu ?? menus[0], MinHeight = 42,
            ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<Menu>((m, _) => Ui.Text(m?.Name ?? "")) };
        menu.IsEnabled = listing is null;
        var price = Ui.Number(listing?.RescuePrice ?? menus[0].NormalPrice / 2, 10000000, 1000);
        var stock = Ui.Number(listing?.Quantity ?? 1, 100000); stock.Minimum = 1;
        var start = listing?.PickupStart.ToLocalTime() ?? DateTime.Now.AddMinutes(-5);
        var end = listing?.PickupEnd.ToLocalTime() ?? DateTime.Now.AddHours(2);
        if (end.Date != start.Date) end = start.Date.AddDays(1).AddSeconds(-1);
        var date = new DatePicker { SelectedDate = new DateTimeOffset(start.Date) };
        var from = new TimePicker { SelectedTime = new TimeSpan(start.Hour, start.Minute, 0), ClockIdentifier = "24HourClock" };
        var until = new TimePicker { SelectedTime = new TimeSpan(end.Hour, end.Minute, 0), ClockIdentifier = "24HourClock" };
        var save = Ui.Button("Simpan penawaran", () => host.Run(() =>
        {
            if (date.SelectedDate is null || from.SelectedTime is null || until.SelectedTime is null)
                throw new InvalidOperationException("Lengkapi tanggal dan waktu pengambilan.");
            session.SaveListing(listing, (Menu)menu.SelectedItem!, price.Value ?? 0, (int)(stock.Value ?? 0),
                date.SelectedDate.Value.Date + from.SelectedTime.Value, date.SelectedDate.Value.Date + until.SelectedTime.Value);
        }, host.ShowListings), true);
        var form = Ui.Stack(20, Ui.Field("Menu", menu), Ui.Field("Harga penawaran (Rp)", price), Ui.Field("Stok tersedia (porsi)", stock),
            Ui.Field("Tanggal pengambilan", date), Ui.FormPair(Ui.Field("Mulai", from), Ui.Field("Selesai", until)),
            Ui.SupportingText("Stok tidak boleh melebihi surplus dalam catatan produksi. Jadwal pesanan aktif tidak dapat diubah."),
            Ui.Row(12, save, Ui.Button("Batal", host.ShowListings)));
        Content = Ui.Page(Ui.Heading(listing is null ? "Buat penawaran" : "Ubah penawaran", "Harga khusus dan jadwal pengambilan makanan surplus."), Ui.Form(form));
    }
}

public sealed class ProductionView : UserControl
{
    internal ComboBox MenuInput { get; private set; } = null!;
    internal DatePicker DateInput { get; private set; } = null!;
    internal NumericUpDown ProducedInput { get; private set; } = null!;
    internal NumericUpDown SoldInput { get; private set; } = null!;
    internal Button SaveButton { get; private set; } = null!;
    internal Dictionary<string, TextBlock> FieldErrors { get; } = new();
    internal ProductionView(MainWindow host, DemoSession session)
    {
        var menus = session.Restaurant.Menus.ToList();
        if (menus.Count == 0)
        {
            Content = Ui.Page(Ui.Heading("Catatan produksi", "Tambahkan menu sebelum mencatat produksi."), Ui.Button("Tambah menu", () => host.ShowMenuEditor(), true)); return;
        }
        DateInput = new DatePicker { SelectedDate = DateTimeOffset.Now, MaxYear = DateTimeOffset.Now };
        MenuInput = new ComboBox { ItemsSource = menus, SelectedIndex = 0, MinWidth = 220, MinHeight = 42,
            ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<Menu>((m, _) => Ui.Text(m?.Name ?? "")) };
        ProducedInput = Ui.Number(); SoldInput = Ui.Number();
        var rescued = Ui.SupportingText("");
        var table = Ui.Stack(0);
        var inputs = new Dictionary<string, Control>
        { ["menu"] = MenuInput, ["date"] = DateInput, ["produced"] = ProducedInput, ["sold"] = SoldInput };
        var outlines = new Dictionary<string, Border>();
        var red = Avalonia.Media.Brush.Parse("#B3261E");
        var attempted = false;
        var refreshing = false;
        DateOnly? SelectedDate() => DateInput.SelectedDate is { } date ? DateOnly.FromDateTime(date.Date) : null;
        int? ReadNumber(NumericUpDown input) => int.TryParse(input.Text, input.ParsingNumberStyle, input.NumberFormat, out var value) ? value : null;
        Control Field(string key, string label)
        {
            inputs[key].HorizontalAlignment = inputs[key] is DatePicker or TimePicker ? HorizontalAlignment.Left : HorizontalAlignment.Stretch;
            var error = Ui.FieldError(); error.IsVisible = false;
            FieldErrors[key] = error;
            var outline = new Border { Child = inputs[key], CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(1), BorderBrush = Avalonia.Media.Brushes.Transparent };
            outlines[key] = outline;
            Avalonia.Automation.AutomationProperties.SetName(inputs[key], label);
            return Ui.Stack(8, Ui.Text(label, 14, bold: true), outline, error);
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
                outlines[pair.Key].BorderBrush = invalid ? red : Avalonia.Media.Brushes.Transparent;
                Avalonia.Automation.AutomationProperties.SetHelpText(pair.Value, message ?? "");
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
                rescued.Text = $"Terselamatkan: {record?.RescuedQuantity ?? 0} porsi. Bertambah setelah kode pengambilan diverifikasi.";
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
                if (first is NumericUpDown number) number.GetVisualDescendants().OfType<TextBox>().FirstOrDefault()?.Focus(); else first.Focus();
                return;
            }
            host.Run(() => session.SaveProduction((Menu)MenuInput.SelectedItem!, SelectedDate()!.Value,
                ReadNumber(ProducedInput)!.Value, ReadNumber(SoldInput)!.Value), Refresh);
        }, true);
        var editor = Ui.Stack(20, Ui.FormPair(Field("menu", "Menu"), Field("date", "Tanggal produksi")),
            Ui.FormPair(Field("produced", "Diproduksi (porsi)"), Field("sold", "Terjual reguler (porsi)")),
            rescued, SaveButton);
        DateInput.SelectedDateChanged += (_, _) => Refresh(); MenuInput.SelectionChanged += (_, _) => Refresh();
        foreach (var number in new[] { ProducedInput, SoldInput })
            number.PropertyChanged += (_, e) =>
            {
                if (e.Property == NumericUpDown.TextProperty || e.Property == NumericUpDown.ValueProperty) Revalidate();
            };
        Refresh();
        Content = Ui.Page(Ui.Heading("Catatan produksi", "Catat produksi dan penjualan reguler untuk menghitung surplus per menu."), Ui.Form(editor),
            Ui.Divider(), Ui.Text("Ringkasan tanggal terpilih", 20, bold: true), table);
    }
}