using System.Globalization;
using Replate.Domain.Entities;
using Replate.Domain.Services;
using Replate.Domain.Enums;

namespace Replate.Desktop;

// All data is local demonstration data; no database or payment service is used.
public sealed class DemoSession
{
    public Restaurant Restaurant { get; private set; } = new() { Name = "Dapur Bumi", Address = "Jl. Kaliurang, Yogyakarta" };
    public Cart Cart { get; private set; } = new() { CustomerId = Guid.NewGuid() };
    public List<SurplusListing> Listings { get; private set; } = [];
    public Order? LastOrder { get; private set; }
    public static string Rupiah(decimal value) => "Rp" + value.ToString("N0", CultureInfo.GetCultureInfo("id-ID"));

    public string? StorePath { get; }
    public string? StorageError { get; private set; }
    public static string DefaultStorePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Replate", "state.json");
    public IEnumerable<Order> CustomerOrders => Restaurant.Orders.Where(o => o.CustomerId == Cart.CustomerId).OrderByDescending(o => o.OrderedAt);

    public DemoSession(string? storePath = null)
    {
        StorePath = storePath;
        if (storePath is not null && File.Exists(storePath))
        {
            try { SessionStore.Load(this, storePath); return; }
            catch (Exception e) when (e is System.Text.Json.JsonException or IOException or ArgumentException or KeyNotFoundException or InvalidOperationException or NullReferenceException)
            { StorageError = "Data lokal tidak dapat dibaca. Berkas asli dipertahankan; perubahan dinonaktifkan. " + e.Message; }
        }

        AddMenu("Chicken rice bowl", "Nasi hangat, ayam panggang, dan sayuran segar.", "Makanan utama", 32000, 16000, 6, 50, 36);
        AddMenu("Butter croissant", "Croissant berlapis dengan mentega, dipanggang hari ini.", "Roti & kue", 24000, 12000, 4, 30, 23);
        AddMenu("Garden salad", "Sayuran renyah dengan dressing wijen yang ringan.", "Vegetarian", 28000, 14000, 5, 26, 20);
        AddMenu("Beef teriyaki bowl", "Daging teriyaki, nasi, dan sayuran pelengkap.", "Makanan utama", 40000, 20000, 3, 40, 35);
    }

    private void AddMenu(string name, string description, string category, decimal normal, decimal rescue,
        int stock, int produced, int sold)
    {
        var menu = new Menu { Name = name, Description = description, Category = category,
            NormalPrice = normal, WeightPerPortion = .3m, RestaurantId = Restaurant.Id, Restaurant = Restaurant };
        Restaurant.Menus.Add(menu);
        var listing = new SurplusListing { Menu = menu, MenuId = menu.Id, Quantity = stock, RescuePrice = rescue,
            PickupStart = DateTime.UtcNow.AddHours(-1), PickupEnd = DateTime.UtcNow.AddHours(4) };
        Listings.Add(listing);
        menu.SurplusListings.Add(listing);
        menu.ProductionRecords.Add(new ProductionRecord { MenuId = menu.Id, Menu = menu,
            ProducedQuantity = produced, RegularSoldQuantity = sold });
        for (var day = 28; day >= 1; day--)
        {
            var regular = sold + day % 3 - 1;
            var rescued = Math.Max(0, produced - regular - 2);
            menu.ProductionRecords.Add(new ProductionRecord { MenuId = menu.Id, Menu = menu,
                RecordDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-day)), ProducedQuantity = produced,
                RegularSoldQuantity = regular, RescuedQuantity = rescued });
            var order = new Order { RestaurantId = Restaurant.Id, TotalPrice = rescue * rescued,
                OrderedAt = DateTime.Today.AddDays(-day).AddHours(12).ToUniversalTime() };
            order.Items.Add(new OrderItem { Listing = listing, ListingId = listing.Id, MenuName = name,
                Quantity = rescued, UnitPrice = rescue });
            order.Confirm(); order.MarkReady(); order.Complete();
            Restaurant.Orders.Add(order);
        }
    }

    public IEnumerable<SurplusListing> Search(string query, string category) =>
        new MarketplaceService(Listings).GetActiveListings().Where(l =>
            l.Menu.IsActive && (category == "Semua" || l.Menu.Category == category) &&
            (l.Menu.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             Restaurant.Name.Contains(query, StringComparison.OrdinalIgnoreCase)));

    public Order Checkout()
    {
        EnsureWritable();
        if (Cart.Items.Any(i => !i.Listing.Menu.IsActive)) throw new InvalidOperationException("Ada menu yang sudah tidak aktif. Hapus menu tersebut dari keranjang.");
        LastOrder = new CheckoutService(new StockService()).CreateOrder(Cart);
        Restaurant.Orders.Add(LastOrder);
        Persist();
        return LastOrder;
    }

    internal void Restore(Restaurant restaurant, Cart cart, List<SurplusListing> listings)
    { Restaurant = restaurant; Cart = cart; Listings = listings; }

    public void EnsureWritable()
    {
        if (StorageError is not null) throw new InvalidOperationException(StorageError);
    }

    public void Persist()
    {
        EnsureWritable();
        if (StorePath is not null) SessionStore.Save(this, StorePath);
    }

    public void AddToCart(SurplusListing listing, int quantity)
    {
        EnsureWritable();
        if (!listing.Menu.IsActive) throw new InvalidOperationException("Menu ini sudah tidak aktif.");
        Cart.AddItem(listing, quantity); Persist();
    }

    public void ChangeQuantity(Guid listingId, int quantity)
    {
        EnsureWritable();
        if (quantity == 0) Cart.RemoveItem(listingId); else Cart.UpdateQuantity(listingId, quantity);
        Persist();
    }

    internal Dictionary<string, string> ValidateMenu(Menu? existing, string name, string description, decimal? normalPrice, decimal? weight)
    {
        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(name)) errors["name"] = "Nama menu wajib diisi.";
        else if (name.Trim().Length > 80) errors["name"] = "Nama menu maksimal 80 karakter.";
        if (description.Length > 500) errors["description"] = "Deskripsi maksimal 500 karakter.";
        if (normalPrice is null) errors["price"] = "Harga wajib diisi.";
        else if (normalPrice <= 0) errors["price"] = "Harga harus lebih dari nol.";
        else if (existing is not null && Listings.Any(l => l.MenuId == existing.Id && l.Status == ListingStatus.Active && l.RescuePrice > normalPrice))
            errors["price"] = "Harga normal tidak boleh lebih rendah dari harga penawaran aktif. Ubah penawaran terlebih dahulu.";
        if (weight is null) errors["weight"] = "Berat per porsi wajib diisi.";
        else if (weight <= 0) errors["weight"] = "Berat per porsi harus lebih dari nol.";
        return errors;
    }

    internal string PhotoDirectory => Path.Combine(Path.GetDirectoryName(Path.GetFullPath(StorePath ?? DefaultStorePath))!, "menu-photos");
    internal string? PhotoPath(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference) || !reference.StartsWith("menu-photos/", StringComparison.Ordinal)) return null;
        var name = reference["menu-photos/".Length..];
        return name == Path.GetFileName(name) && !name.Contains((char)92) && !name.Contains('/')
            ? Path.Combine(PhotoDirectory, name) : null;
    }

    public Menu SaveMenu(Menu? existing, string name, string description, string category, decimal normalPrice, decimal weight, MenuPhotoChange? photoChange = null)
    {
        EnsureWritable();
        var errors = ValidateMenu(existing, name, description, normalPrice, weight);
        if (errors.Count > 0) throw new InvalidOperationException(errors.First().Value);
        var menu = existing ?? new Menu { RestaurantId = Restaurant.Id, Restaurant = Restaurant };
        var old = (menu.Name, menu.Description, menu.Category, menu.NormalPrice, menu.WeightPerPortion, menu.ImageUrl);
        string? copiedPath = null;
        try
        {
            if (photoChange is not null)
            {
                if (photoChange.Data is null) menu.ImageUrl = "";
                else
                {
                    Directory.CreateDirectory(PhotoDirectory);
                    var fileName = Guid.NewGuid().ToString("N") + photoChange.Extension;
                    copiedPath = Path.Combine(PhotoDirectory, fileName);
                    using (var file = new FileStream(copiedPath, FileMode.CreateNew, FileAccess.Write))
                        file.Write(photoChange.Data);
                    menu.ImageUrl = "menu-photos/" + fileName;
                }
            }
            menu.UpdateMenu(name.Trim(), description.Trim(), category, normalPrice, weight);
            if (existing is null) Restaurant.Menus.Add(menu);
            Persist(); return menu;
        }
        catch
        {
            if (existing is null) Restaurant.Menus.Remove(menu);
            menu.UpdateMenu(old.Name, old.Description, old.Category, old.NormalPrice, old.WeightPerPortion);
            menu.ImageUrl = old.ImageUrl;
            if (copiedPath is not null)
                try { File.Delete(copiedPath); } catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
            throw;
        }
    }

    public void DeleteMenu(Menu menu)
    {
        EnsureWritable();
        var index = Restaurant.Menus.FindIndex(m => m.Id == menu.Id);
        if (index < 0) throw new InvalidOperationException("Menu tidak ditemukan.");
        var stored = Restaurant.Menus[index];
        if (stored.SurplusListings.Count > 0 || Listings.Any(l => l.MenuId == stored.Id || l.Menu.Id == stored.Id) ||
            Cart.Items.Any(i => i.Listing.MenuId == stored.Id || i.Listing.Menu.Id == stored.Id) ||
            Restaurant.Orders.Any(o => o.Items.Any(i => i.Listing.MenuId == stored.Id || i.Listing.Menu.Id == stored.Id)) ||
            Restaurant.Menus.SelectMany(m => m.ProductionRecords).Any(r => r.MenuId == stored.Id || r.Menu?.Id == stored.Id) ||
            stored.ProductionRecords.Count > 0)
            throw new InvalidOperationException("Menu tidak dapat dihapus karena masih digunakan oleh penawaran surplus, keranjang, pesanan, atau catatan produksi. Gunakan Nonaktifkan agar riwayat tetap tersimpan.");
        Restaurant.Menus.RemoveAt(index);
        try { Persist(); }
        catch { Restaurant.Menus.Insert(index, stored); throw; }
    }

    public void ToggleMenu(Menu menu)
    {
        EnsureWritable();
        if (menu.IsActive) menu.Deactivate(); else menu.Activate();
        Persist();
    }

    public SurplusListing SaveListing(SurplusListing? existing, Menu menu, decimal price, int quantity, DateTime start, DateTime end)
    {
        EnsureWritable();
        if (!menu.IsActive) throw new InvalidOperationException("Aktifkan menu sebelum membuat penawaran.");
        if (price <= 0 || price > menu.NormalPrice) throw new InvalidOperationException("Harga penawaran harus lebih dari nol dan tidak melebihi harga normal.");
        if (quantity < 1) throw new InvalidOperationException("Stok tersedia harus minimal satu porsi.");
        if (end <= start || end <= DateTime.Now || start.Date != end.Date) throw new InvalidOperationException("Jadwal pengambilan harus dalam satu hari, waktu selesai sesudah mulai dan belum lewat.");
        var date = DateOnly.FromDateTime(start);
        var record = menu.ProductionRecords.SingleOrDefault(r => r.RecordDate == date)
            ?? throw new InvalidOperationException("Catat produksi menu untuk tanggal pengambilan terlebih dahulu.");
        var allocated = Allocated(menu, date, existing);
        if (quantity + allocated > record.CalculateSurplus() - record.RescuedQuantity)
            throw new InvalidOperationException("Stok melebihi surplus yang belum dialokasikan. Periksa catatan produksi dan penawaran lain.");
        if (existing is not null && existing.Status is ListingStatus.Cancelled or ListingStatus.Expired)
            throw new InvalidOperationException("Penawaran yang berakhir tidak dapat diubah. Buat penawaran baru.");
        if (existing is not null && Restaurant.Orders.Any(o => o.Status is not (OrderStatus.Completed or OrderStatus.Cancelled) && o.Items.Any(i => i.ListingId == existing.Id)) &&
            (ToMinute(existing.PickupStart.ToLocalTime()) != ToMinute(start) || ToMinute(existing.PickupEnd.ToLocalTime()) != ToMinute(end)))
            throw new InvalidOperationException("Jadwal tidak dapat diubah saat masih ada pesanan aktif.");
        var listing = existing ?? new SurplusListing { Menu = menu, MenuId = menu.Id };
        listing.RescuePrice = price; listing.PickupStart = start.ToUniversalTime(); listing.PickupEnd = end.ToUniversalTime(); listing.UpdateStock(quantity);
        if (existing is null) { Listings.Add(listing); menu.SurplusListings.Add(listing); }
        Persist(); return listing;
    }

    private static DateTime ToMinute(DateTime value) => new(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0);

    private int Allocated(Menu menu, DateOnly date, SurplusListing? except = null) =>
        Listings.Where(l => l.MenuId == menu.Id && l.Id != except?.Id && l.Status == ListingStatus.Active &&
            DateOnly.FromDateTime(l.PickupStart.ToLocalTime()) == date && l.PickupEnd > DateTime.UtcNow).Sum(l => l.Quantity)
        + Restaurant.Orders.Where(o => o.Status is not (OrderStatus.Completed or OrderStatus.Cancelled))
            .SelectMany(o => o.Items).Where(i => i.Listing.MenuId == menu.Id && DateOnly.FromDateTime(i.Listing.PickupStart.ToLocalTime()) == date).Sum(i => i.Quantity);

    public void CancelListing(SurplusListing listing)
    { EnsureWritable(); listing.Cancel(); Persist(); }

    internal Dictionary<string, string> ValidateProduction(Menu? menu, DateOnly? date, int? produced, int? regularSold)
    {
        var errors = new Dictionary<string, string>();
        if (menu is null) errors["menu"] = "Pilih menu.";
        if (date is null) errors["date"] = "Tanggal produksi wajib diisi.";
        else if (date > DateOnly.FromDateTime(DateTime.Today)) errors["date"] = "Tanggal produksi tidak boleh di masa depan.";
        if (produced is null) errors["produced"] = "Jumlah produksi wajib diisi.";
        else if (produced < 0) errors["produced"] = "Produksi tidak boleh negatif.";
        if (regularSold is null) errors["sold"] = "Jumlah penjualan reguler wajib diisi.";
        else if (regularSold < 0) errors["sold"] = "Penjualan reguler tidak boleh negatif.";
        else if (produced is not null && produced >= 0 && regularSold > produced)
            errors["sold"] = "Penjualan reguler tidak boleh melebihi produksi.";
        if (menu is not null && date is not null && produced >= 0 && regularSold >= 0)
        {
            var rescued = menu.ProductionRecords.SingleOrDefault(r => r.RecordDate == date)?.RescuedQuantity ?? 0;
            var allocated = Allocated(menu, date.Value);
            var minimum = (long)regularSold.Value + rescued + allocated;
            if (minimum > produced)
                errors["produced"] = $"Jumlah produksi minimal {minimum} porsi.";
        }
        return errors;
    }

    public void SaveProduction(Menu menu, DateOnly date, int produced, int regularSold)
    {
        EnsureWritable();
        var errors = ValidateProduction(menu, date, produced, regularSold);
        if (errors.Count > 0) throw new InvalidOperationException(errors.First().Value);
        var record = menu.ProductionRecords.SingleOrDefault(r => r.RecordDate == date)
            ?? new ProductionRecord { MenuId = menu.Id, Menu = menu, RecordDate = date };
        record.ProducedQuantity = produced; record.RegularSoldQuantity = regularSold;
        if (!menu.ProductionRecords.Contains(record)) menu.ProductionRecords.Add(record);
        Persist();
    }
    public void ConfirmOrder(Order order, int preparationMinutes)
    {
        EnsureWritable();
        if (preparationMinutes is < 1 or > 240) throw new InvalidOperationException("Waktu persiapan harus antara 1 dan 240 menit.");
        order.Confirm(); order.SetPreparationTime(TimeSpan.FromMinutes(preparationMinutes)); Persist();
    }

    public void ReadyOrder(Order order)
    { EnsureWritable(); order.MarkReady(); Persist(); }

    public void CancelOrder(Order order)
    {
        EnsureWritable(); order.Cancel();
        foreach (var item in order.Items) new StockService().RestoreStock(item.Listing, item.Quantity);
        Persist();
    }

    public void CompletePickup(Order order, string code)
    {
        EnsureWritable();
        if (order.Status != OrderStatus.ReadyForPickup) throw new InvalidOperationException("Pesanan belum siap diambil atau sudah selesai.");
        var verification = new PickupVerification { Order = order, OrderId = order.Id, EnteredCode = code.Trim().ToUpperInvariant() };
        if (!verification.VerifyCode()) throw new InvalidOperationException("Kode pengambilan tidak cocok. Periksa kode dari pelanggan.");
        var changes = order.Items.GroupBy(i => (i.Listing.Menu, DateOnly.FromDateTime(i.Listing.PickupStart.ToLocalTime())))
            .Select(g => (Record: g.Key.Menu.ProductionRecords.SingleOrDefault(r => r.RecordDate == g.Key.Item2)
                ?? throw new InvalidOperationException("Catatan produksi untuk pesanan ini tidak ditemukan."), Quantity: g.Sum(i => i.Quantity))).ToList();
        if (changes.Any(c => c.Record.RescuedQuantity + c.Quantity > c.Record.CalculateSurplus()))
            throw new InvalidOperationException("Porsi terselamatkan melebihi catatan surplus. Periksa catatan produksi.");
        verification.CompletePickup();
        foreach (var change in changes) change.Record.UpdateRescuedQuantity(change.Record.RescuedQuantity + change.Quantity);
        Persist();
    }

    public static string Status(Order order) => order.Status switch
    {
        OrderStatus.Reserved => "Menunggu Konfirmasi", OrderStatus.Confirmed => "Diproses",
        OrderStatus.ReadyForPickup => "Siap Diambil", OrderStatus.Completed => "Selesai", _ => "Dibatalkan"
    };
}
