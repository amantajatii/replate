using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
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
            list.Children.Add(Ui.TableRow("*,140,100,220", false,
                Ui.Stack(6, Ui.Text(menu.Name, 18, bold: true), Ui.Text(menu.Category, 13, Ui.Muted), Ui.Text(menu.Description, 13, Ui.Muted)),
                Ui.Text(DemoSession.Rupiah(menu.NormalPrice), 14), Ui.Text(menu.IsActive ? "Aktif" : "Nonaktif", 13),
                Ui.Row(8, Ui.Button("Ubah", () => host.ShowMenuEditor(menu)),
                    Ui.Button(menu.IsActive ? "Nonaktifkan" : "Aktifkan", () => host.Run(() => session.ToggleMenu(menu), host.ShowMenus)))));
        if (list.Children.Count == 0) list.Children.Add(Ui.Text("Belum ada menu. Tambahkan menu untuk mulai mencatat produksi."));
        Content = Ui.Page(Ui.Heading("Menu restoran", "Daftar menu, harga normal, dan berat per porsi.",
            Ui.Button("Tambah menu", () => host.ShowMenuEditor(), true)), list);
    }
}

public sealed class MenuEditorView : UserControl
{
    internal TextBox NameInput { get; }
    internal Button SaveButton { get; }
    internal MenuEditorView(MainWindow host, DemoSession session, Menu? menu)
    {
        NameInput = Ui.Input(menu?.Name ?? "");
        var description = Ui.Input(menu?.Description ?? "", true);
        var category = new ComboBox { ItemsSource = new[] { "Makanan utama", "Roti & kue", "Vegetarian" }, SelectedItem = menu?.Category ?? "Makanan utama", MinHeight = 42 };
        var price = Ui.Number(menu?.NormalPrice ?? 20000, 10000000, 1000);
        var weight = Ui.Number(menu?.WeightPerPortion ?? .3m, 100, .05m);
        SaveButton = Ui.Button("Simpan menu", () => host.Run(() => session.SaveMenu(menu, NameInput.Text ?? "", description.Text ?? "",
            category.SelectedItem?.ToString() ?? "Makanan utama", price.Value ?? 0, weight.Value ?? 0), host.ShowMenus), true);
        var form = Ui.Stack(20, Ui.Field("Nama menu", NameInput), Ui.Field("Deskripsi", description), Ui.Field("Kategori", category),
            Ui.Field("Harga normal (Rp)", price), Ui.Field("Berat per porsi (kg)", weight),
            Ui.Row(12, SaveButton, Ui.Button("Batal", host.ShowMenus)));
        form.Width = 640; form.HorizontalAlignment = HorizontalAlignment.Left;
        Content = Ui.Page(Ui.Heading(menu is null ? "Tambah menu" : "Ubah menu", "Menu dipakai untuk pencatatan produksi dan penawaran surplus."), form);
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
            Ui.Field("Tanggal pengambilan", date), Ui.Row(24, Ui.Field("Mulai", from), Ui.Field("Selesai", until)),
            Ui.Text("Stok tidak boleh melebihi surplus dalam catatan produksi. Jadwal pesanan aktif tidak dapat diubah.", 13, Ui.Muted),
            Ui.Row(12, save, Ui.Button("Batal", host.ShowListings)));
        form.Width = 640; form.HorizontalAlignment = HorizontalAlignment.Left;
        Content = Ui.Page(Ui.Heading(listing is null ? "Buat penawaran" : "Ubah penawaran", "Harga khusus dan jadwal pengambilan makanan surplus."), form);
    }
}

public sealed class ProductionView : UserControl
{
    internal ProductionView(MainWindow host, DemoSession session)
    {
        var menus = session.Restaurant.Menus.ToList();
        if (menus.Count == 0)
        {
            Content = Ui.Page(Ui.Heading("Catatan produksi", "Tambahkan menu sebelum mencatat produksi."), Ui.Button("Tambah menu", () => host.ShowMenuEditor(), true)); return;
        }
        var date = new DatePicker { SelectedDate = DateTimeOffset.Now, MaxYear = DateTimeOffset.Now };
        var menu = new ComboBox { ItemsSource = menus, SelectedIndex = 0, MinWidth = 220, MinHeight = 42,
            ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<Menu>((m, _) => Ui.Text(m?.Name ?? "")) };
        var produced = Ui.Number(); var sold = Ui.Number();
        var rescued = Ui.Text("", 13, Ui.Muted);
        var table = Ui.Stack(0);
        DateOnly SelectedDate() => DateOnly.FromDateTime(date.SelectedDate?.Date ?? DateTime.Today);
        void Refresh()
        {
            var selected = (Menu)menu.SelectedItem!;
            var record = selected.ProductionRecords.SingleOrDefault(r => r.RecordDate == SelectedDate());
            produced.Value = record?.ProducedQuantity ?? 0; sold.Value = record?.RegularSoldQuantity ?? 0;
            rescued.Text = $"Terselamatkan: {record?.RescuedQuantity ?? 0} porsi. Bertambah setelah kode pengambilan diverifikasi.";
            table.Children.Clear();
            table.Children.Add(Ui.TableRow("1.6*,*,*,*,*", true, Ui.Text("Menu", 13, Ui.Muted), Ui.Text("Produksi", 13, Ui.Muted),
                Ui.Text("Terjual reguler", 13, Ui.Muted), Ui.Text("Surplus", 13, Ui.Muted), Ui.Text("Terselamatkan", 13, Ui.Muted)));
            foreach (var m in menus)
            {
                var r = m.ProductionRecords.SingleOrDefault(r => r.RecordDate == SelectedDate());
                table.Children.Add(Ui.TableRow("1.6*,*,*,*,*", false, Ui.Text(m.Name, 14, bold: true),
                    Ui.Text((r?.ProducedQuantity ?? 0).ToString()), Ui.Text((r?.RegularSoldQuantity ?? 0).ToString()),
                    Ui.Text((r?.CalculateSurplus() ?? 0).ToString()), Ui.Text((r?.RescuedQuantity ?? 0).ToString())));
            }
        }
        date.SelectedDateChanged += (_, _) => Refresh(); menu.SelectionChanged += (_, _) => Refresh(); Refresh();
        var editor = Ui.Stack(20, Ui.Columns("*,24,*", Ui.Field("Menu", menu), new Border(), Ui.Field("Tanggal produksi", date)),
            Ui.Columns("*,24,*", Ui.Field("Diproduksi (porsi)", produced), new Border(), Ui.Field("Terjual reguler (porsi)", sold)),
            rescued, Ui.Button("Simpan catatan", () => host.Run(() => session.SaveProduction((Menu)menu.SelectedItem!, SelectedDate(),
                (int)(produced.Value ?? 0), (int)(sold.Value ?? 0)), Refresh), true));
        editor.Width = 760; editor.HorizontalAlignment = HorizontalAlignment.Left;
        Content = Ui.Page(Ui.Heading("Catatan produksi", "Catat produksi dan penjualan reguler untuk menghitung surplus per menu."), editor,
            Ui.Divider(), Ui.Text("Ringkasan tanggal terpilih", 20, bold: true), table);
    }
}
