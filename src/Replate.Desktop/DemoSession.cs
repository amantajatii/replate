using System.Globalization;
using Replate.Domain.Entities;
using Replate.Domain.Services;
using Replate.Domain.Enums;
using Replate.Desktop.Data;

namespace Replate.Desktop;

public sealed class DemoSession
{
    public List<Restaurant> Restaurants { get; private set; } = [];
    public Restaurant Restaurant { get; private set; } = null!;
    public Cart Cart { get; private set; } = new();
    public List<Cart> Carts { get; private set; } = [];
    public List<User> Users { get; private set; } = [];
    public List<SurplusListing> Listings { get; private set; } = [];
    public Order? LastOrder { get; private set; }
    internal IOrderStore? OrderStore { get; set; }
    public User? CurrentUser { get; private set; }
    private SupabaseAuth.Session? supabaseSession;
    private readonly HashSet<Guid> customerApiOrderIds = [];
    private readonly HashSet<Guid> restaurantApiOrderIds = [];
    public bool IsSignedIn => CurrentUser is not null;
    public static string Rupiah(decimal value) => "Rp" + value.ToString("N0", CultureInfo.GetCultureInfo("id-ID"));

    public string? StorePath { get; }
    public string? StorageError { get; private set; }
    public static string DefaultStorePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Replate", "state.json");
    public IEnumerable<Order> AllOrders => Restaurants.SelectMany(r => r.Orders);
    public IEnumerable<Order> CustomerOrders => CurrentUser is Customer customer
        ? AllOrders.Where(o => o.CustomerId == customer.Id &&
            (supabaseSession is null || customerApiOrderIds.Contains(o.Id))).OrderByDescending(o => o.OrderedAt) : [];
    public IEnumerable<SurplusListing> RestaurantListings => Listings.Where(l => l.Menu.RestaurantId == Restaurant.Id);
    internal IEnumerable<Order> RestaurantOrders => Restaurant.Orders.Where(CanViewRestaurantOrder);
    internal bool CanViewRestaurantOrder(Order order) =>
        supabaseSession is null || restaurantApiOrderIds.Contains(order.Id);
    public Restaurant? FindRestaurant(Guid id) => Restaurants.FirstOrDefault(r => r.Id == id);
    public string RestaurantLine(Guid restaurantId) =>
        FindRestaurant(restaurantId) is { } restaurant ? $"{restaurant.Name} · {restaurant.Address}" : "Restoran tidak ditemukan";

    public DemoSession(string? storePath = null)
    {
        StorePath = storePath;
        if (storePath is not null && File.Exists(storePath))
        {
            try { SessionStore.Load(this, storePath); DemoAccounts.Ensure(Users, Restaurants.Where(r => !ImportedRestaurantIds.Contains(r.Id)), Carts); return; }
            catch (Exception e) when (e is System.Text.Json.JsonException or IOException or InvalidDataException or ArgumentException or KeyNotFoundException or InvalidOperationException or NullReferenceException)
            { StorageError = "Data lokal tidak dapat dibaca. Berkas asli dipertahankan; perubahan dinonaktifkan. " + e.Message; }
        }

        var bumi = AddRestaurant("Dapur Bumi", "Jl. Kaliurang, Yogyakarta", "Rice bowl, salad, dan roti harian.",
            "0274 555 0101", new TimeOnly(9, 0), new TimeOnly(21, 0));
        AddMenu(bumi, "Chicken rice bowl", "Nasi hangat, ayam panggang, dan sayuran segar.", "Makanan utama", 32000, 16000, 6, 50, 36);
        AddMenu(bumi, "Butter croissant", "Croissant berlapis dengan mentega, dipanggang hari ini.", "Roti & kue", 24000, 12000, 4, 30, 23);
        AddMenu(bumi, "Garden salad", "Sayuran renyah dengan dressing wijen yang ringan.", MenuCategories.Produce, 28000, 14000, 5, 26, 20);
        AddMenu(bumi, "Beef teriyaki bowl", "Daging teriyaki, nasi, dan sayuran pelengkap.", "Makanan utama", 40000, 20000, 3, 40, 35);
        var sambal = AddRestaurant("Warung Sambal Ijo", "Jl. Gejayan No. 21, Yogyakarta", "Masakan rumahan dengan sambal ijo.",
            "0274 555 0202", new TimeOnly(10, 0), new TimeOnly(22, 0));
        AddMenu(sambal, "Nasi ayam geprek", "Ayam goreng tepung, sambal ijo, dan nasi putih.", "Makanan utama", 25000, 12000, 7, 60, 48);
        AddMenu(sambal, "Gado-gado", "Sayuran rebus, tahu, tempe, dan bumbu kacang.", MenuCategories.Produce, 22000, 11000, 4, 30, 24);
        AddMenu(sambal, "Soto ayam", "Kuah kuning, suwiran ayam, soun, dan perkedel.", "Makanan utama", 20000, 9000, 5, 45, 38);
        var pagi = AddRestaurant("Roti Pagi Kotabaru", "Jl. Suroto No. 8, Kotabaru, Yogyakarta", "Roti dan kue yang dipanggang setiap pagi.",
            "0274 555 0303", new TimeOnly(6, 0), new TimeOnly(18, 0));
        AddMenu(pagi, "Roti sobek cokelat", "Roti lembut isi cokelat, dipanggang pagi ini.", "Roti & kue", 18000, 8000, 6, 40, 31);
        AddMenu(pagi, "Bolu pandan", "Bolu pandan potong dengan aroma daun pandan asli.", "Roti & kue", 30000, 14000, 3, 20, 16);
        AddMenu(pagi, "Lumpia sayur", "Lumpia goreng isi rebung, wortel, dan kol.", MenuCategories.Produce, 15000, 7000, 8, 50, 40);
        Restaurant = bumi;
        DemoAccounts.Ensure(Users, Restaurants, Carts);
    }

    private Restaurant AddRestaurant(string name, string address, string description, string phone, TimeOnly opening, TimeOnly closing)
    {
        var restaurant = new Restaurant { Name = name, Address = address, Description = description, Phone = phone,
            OpeningTime = opening, ClosingTime = closing };
        Restaurants.Add(restaurant);
        return restaurant;
    }

    private void AddMenu(Restaurant restaurant, string name, string description, string category, decimal normal, decimal rescue,
        int stock, int produced, int sold)
    {
        var menu = new Menu { Name = name, Description = description, Category = category,
            NormalPrice = normal, WeightPerPortion = .3m, RestaurantId = restaurant.Id, Restaurant = restaurant };
        restaurant.Menus.Add(menu);
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
            var order = new Order { RestaurantId = restaurant.Id, TotalPrice = rescue * rescued,
                OrderedAt = DateTime.Today.AddDays(-day).AddHours(12).ToUniversalTime() };
            order.Items.Add(new OrderItem { Listing = listing, ListingId = listing.Id, MenuName = name,
                Quantity = rescued, UnitPrice = rescue });
            order.Confirm(); order.MarkReady(); order.Complete();
            restaurant.Orders.Add(order);
        }
    }

    public void SelectRestaurant(Guid restaurantId) => Restaurant = RequireOwnedRestaurant(restaurantId);

    // Accounts
    internal Dictionary<string, string> ValidateRegistration(UserRole role, string name, string email, string password, RestaurantProfile? restaurant = null) =>
        role == UserRole.RestaurantOwner
            ? AccountService.ValidateOwnerRegistration(Users, name, email, password, restaurant)
            : AccountService.ValidateAccount(Users, name, email, password);

    public User Register(UserRole role, string name, string email, string password, RestaurantProfile? restaurant = null)
    {
        EnsureWritable();
        if (UsesSharedStock && SupabaseAuth.IsConfigured)
            throw new InvalidOperationException("Buat akun melalui administrator Supabase, lalu masuk di sini.");
        var errors = ValidateRegistration(role, name, email, password, restaurant);
        if (errors.Count > 0) throw new InvalidOperationException(errors.First().Value);
        User user = role == UserRole.RestaurantOwner
            ? AccountService.CreateOwner(Users, name, email, password, restaurant!)
            : AccountService.CreateCustomer(Users, name, email, password);
        var created = user is RestaurantOwner owner ? owner.Restaurants.Single() : null;
        Users.Add(user);
        if (created is not null) Restaurants.Add(created);
        try { Persist(); }
        catch
        {
            Users.Remove(user);
            if (created is not null) Restaurants.Remove(created);
            throw;
        }
        return user;
    }

    public User SignIn(string email, string password)
    {
        var session = UsesSharedStock && SupabaseAuth.IsConfigured
            ? SupabaseAuth.SignInAsync(email, password).GetAwaiter().GetResult()
            : null;
        var user = session is not null
            ? SignInWithSupabase(session.User)
            : AccountService.Authenticate(Users, email, password)
                ?? throw new InvalidOperationException(AccountService.SignInFailedMessage);
        if (user is RestaurantOwner owner)
        {
            Restaurant = owner.Restaurants.FirstOrDefault(Restaurants.Contains)
                ?? throw new InvalidOperationException("Akun pemilik ini belum memiliki restoran.");
            Cart = new Cart();
        }
        else if (user is Customer customer)
        {
            var cart = Carts.FirstOrDefault(c => c.CustomerId == customer.Id);
            if (cart is null) { cart = new Cart { CustomerId = customer.Id }; Carts.Add(cart); }
            Cart = customer.Cart = cart;
        }
        if (CurrentUser != user) CurrentUser?.Logout();
        supabaseSession = session;
        customerApiOrderIds.Clear();
        restaurantApiOrderIds.Clear();
        CurrentUser = user; LastOrder = null;
        return user;
    }

    private User SignInWithSupabase(SupabaseAuth.Identity identity)
    {
        var owned = new DatabaseService().LoadOwnedRestaurants(identity.Id);
        if (owned.Count == 0)
            return new Customer { Id = identity.Id, Name = identity.Email, Email = identity.Email };

        var owner = new RestaurantOwner { Id = identity.Id, Name = identity.Email, Email = identity.Email };
        foreach (var remote in owned)
        {
            var restaurant = FindRestaurant(remote.Id);
            if (restaurant is null)
            {
                restaurant = remote;
                Restaurants.Add(restaurant);
            }

            restaurant.Name = remote.Name;
            restaurant.Address = remote.Address;
            restaurant.Owner?.Restaurants.Remove(restaurant);
            restaurant.Owner = owner;
            restaurant.OwnerId = owner.Id;
            owner.Restaurants.Add(restaurant);
            ImportedRestaurantIds.Add(restaurant.Id);
        }

        return owner;
    }

    public void SignOut()
    {
        CurrentUser?.Logout();
        supabaseSession = null;
        customerApiOrderIds.Clear();
        restaurantApiOrderIds.Clear();
        CurrentUser = null; Cart = new Cart(); LastOrder = null;
    }

    private Customer RequireCustomer() =>
        CurrentUser as Customer ?? throw new InvalidOperationException("Masuk sebagai pelanggan untuk melanjutkan.");

    private RestaurantOwner RequireOwner() =>
        CurrentUser as RestaurantOwner ?? throw new InvalidOperationException("Masuk sebagai pemilik restoran untuk melanjutkan.");

    private Restaurant RequireOwnedRestaurant(Guid restaurantId)
    {
        var owner = RequireOwner();
        var restaurant = FindRestaurant(restaurantId);
        return restaurant is not null && restaurant.OwnerId == owner.Id && owner.Restaurants.Contains(restaurant)
            ? restaurant : throw new InvalidOperationException("Restoran ini bukan milik akun Anda.");
    }

    public IEnumerable<SurplusListing> Search(string query, string category) =>
        new MarketplaceService(Listings).GetActiveListings().Where(l =>
            (!UsesSharedStock || sharedListingIds.Contains(l.Id)) && l.Menu.IsActive && (category == "Semua" || l.Menu.Category == category) &&
            (l.Menu.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             l.Menu.Restaurant?.Name.Contains(query, StringComparison.OrdinalIgnoreCase) == true));

    private readonly HashSet<Guid> sharedListingIds = [];
    internal HashSet<Guid> ImportedRestaurantIds { get; } = [];
    private bool UsesSharedStock => StorePath == DefaultStorePath;

    internal void RefreshSharedListings()
    {
        if (!UsesSharedStock || checkingOut) return;

        ApplySharedListings(new DatabaseService().LoadListings());
    }

    internal void RefreshRestaurantOrders()
    {
        restaurantApiOrderIds.Clear();
        if (!UsesSharedStock || checkingOut) return;

        var restaurant = RequireOwnedRestaurant(Restaurant.Id);
        RefreshSharedListings();
        var listings = RestaurantListings.ToDictionary(listing => listing.Id);
        if (supabaseSession is { } session)
        {
            var orders = SupabaseOrderReader.LoadRestaurantOrdersAsync(session, restaurant.Id, listings).GetAwaiter().GetResult();
            MergeSharedOrders(orders, preferRemote: true);
            restaurantApiOrderIds.UnionWith(orders.Select(order => order.Id));
            return;
        }

        MergeSharedOrders(new DatabaseService().LoadOrders(restaurant.Id, listings));
    }

    internal void RefreshCustomerOrders()
    {
        if (!UsesSharedStock || checkingOut) return;

        var customer = RequireCustomer();
        customerApiOrderIds.Clear();
        RefreshSharedListings();
        var listings = Listings.ToDictionary(listing => listing.Id);
        if (supabaseSession is { } session)
        {
            var orders = SupabaseOrderReader.LoadCustomerOrdersAsync(session, listings).GetAwaiter().GetResult();
            MergeSharedOrders(orders, preferRemote: true);
            customerApiOrderIds.UnionWith(orders.Select(order => order.Id));
            return;
        }

        MergeSharedOrders(new DatabaseService().LoadOrders(null, listings, customer.Id));
    }

    private void MergeSharedOrders(IEnumerable<Order> orders, bool preferRemote = false)
    {
        foreach (var order in orders)
        {
            var restaurant = FindRestaurant(order.RestaurantId)!;
            var index = restaurant.Orders.FindIndex(local => local.Id == order.Id);
            if (index < 0)
            {
                restaurant.Orders.Add(order);
                continue;
            }

            var local = restaurant.Orders[index];
            if (!preferRemote && (local.Status is OrderStatus.Completed or OrderStatus.Cancelled || order.Status <= local.Status))
                continue;

            restaurant.Orders[index] = order;
            if (LastOrder?.Id == order.Id) LastOrder = order;
        }
    }

    internal void ApplySharedListings(IEnumerable<SurplusListing> sharedListings)
    {
        sharedListingIds.Clear();
        foreach (var shared in sharedListings)
        {
            var remoteRestaurant = shared.Menu.Restaurant;
            var menu = Restaurants.SelectMany(r => r.Menus).FirstOrDefault(candidate => candidate.Id == shared.MenuId);
            if (remoteRestaurant is not null && menu is not null && menu.RestaurantId != remoteRestaurant.Id)
                continue;

            sharedListingIds.Add(shared.Id);
            var restaurant = remoteRestaurant is null ? null : FindRestaurant(remoteRestaurant.Id);
            if (restaurant is not null && remoteRestaurant is not null && ImportedRestaurantIds.Contains(restaurant.Id))
            {
                restaurant.Name = remoteRestaurant.Name;
                restaurant.Address = remoteRestaurant.Address;
            }

            var listing = Listings.Find(candidate => candidate.Id == shared.Id);
            if (listing is null)
            {
                if (menu is null)
                {
                    if (remoteRestaurant is null) continue;
                    if (restaurant is null)
                    {
                        restaurant = remoteRestaurant;
                        Restaurants.Add(restaurant);
                        ImportedRestaurantIds.Add(restaurant.Id);
                    }

                    menu = shared.Menu;
                    menu.Restaurant = restaurant;
                    menu.RestaurantId = restaurant.Id;
                    restaurant.Menus.Add(menu);
                }

                shared.Menu = menu;
                menu.SurplusListings.Add(shared);
                Listings.Add(shared);
                continue;
            }

            listing.RescuePrice = shared.RescuePrice;
            listing.PickupStart = shared.PickupStart;
            listing.PickupEnd = shared.PickupEnd;
            listing.UpdateStock(shared.Quantity);
            if (shared.Status == ListingStatus.Cancelled) listing.Cancel();
        }
    }

    public Order Checkout(Guid restaurantId)
    {
        var cart = Cart;
        var order = PrepareCheckout(restaurantId);
        try
        {
            if (supabaseSession is { } session)
                return CommitSupabaseCheckout(cart, SupabaseOrderWriter.CheckoutAsync(session, order).GetAwaiter().GetResult());
            OrderStoreForCheckout()?.SaveOrderAsync(order).GetAwaiter().GetResult();
            new CheckoutService(new StockService()).CommitOrder(cart, order);
            return CommitCheckout(cart, order);
        }
        finally { checkingOut = false; }
    }

    public async Task<Order> CheckoutAsync(Guid restaurantId)
    {
        var cart = Cart;
        var order = PrepareCheckout(restaurantId);
        try
        {
            if (supabaseSession is { } session)
                return CommitSupabaseCheckout(cart, await SupabaseOrderWriter.CheckoutAsync(session, order));
            if (OrderStoreForCheckout() is { } store) await store.SaveOrderAsync(order);
            new CheckoutService(new StockService()).CommitOrder(cart, order);
            return CommitCheckout(cart, order);
        }
        finally { checkingOut = false; }
    }

    private bool checkingOut;
    private bool expirationNeedsSave;
    private readonly Dictionary<(Guid CustomerId, Guid RestaurantId), Order> pendingCheckouts = [];

    private IOrderStore? OrderStoreForCheckout() => OrderStore ?? (StorePath == DefaultStorePath ? new DatabaseService() : null);

    private void EnsureNotCheckingOut()
    {
        if (checkingOut) throw new InvalidOperationException("Pesanan sedang diproses. Tunggu sampai selesai.");
    }

    private Order PrepareCheckout(Guid restaurantId)
    {
        EnsureWritable();
        RequireCustomer();
        EnsureNotCheckingOut();
        var group = Cart.FindGroup(restaurantId) ?? throw new InvalidOperationException("Restoran ini tidak ada di keranjang.");
        var checkoutKey = (Cart.CustomerId, restaurantId);
        if (supabaseSession is not null && pendingCheckouts.TryGetValue(checkoutKey, out var pending) &&
            pending.Items.Select(item => (item.ListingId, item.Quantity, item.UnitPrice)).OrderBy(item => item.ListingId)
                .SequenceEqual(group.Items.Select(item => (item.ListingId, item.Quantity, item.UnitPrice)).OrderBy(item => item.ListingId)))
        {
            checkingOut = true;
            return pending;
        }

        if (group.Items.Any(i => !i.Listing.Menu.IsActive)) throw new InvalidOperationException("Ada menu yang sudah tidak aktif. Hapus menu tersebut dari keranjang.");
        if (FindRestaurant(restaurantId) is null)
            throw new InvalidOperationException("Restoran untuk keranjang ini tidak ditemukan. Kosongkan keranjang lalu pilih makanan lagi.");
        var order = new CheckoutService(new StockService()).PrepareOrder(Cart, restaurantId);
        if (UsesSharedStock && OrderStore is null && group.Items.Any(item => !sharedListingIds.Contains(item.ListingId)))
            throw new InvalidOperationException("Ada penawaran lokal di keranjang. Hapus item tersebut lalu pilih kembali dari Jelajah.");
        if (supabaseSession is not null) pendingCheckouts[checkoutKey] = order;
        checkingOut = true;
        return order;
    }

    private Order CommitSupabaseCheckout(Cart cart, (Order Order, Dictionary<Guid, int> Stock) checkout)
    {
        var order = checkout.Order;
        foreach (var item in order.Items)
        {
            if (checkout.Stock.TryGetValue(item.ListingId, out var quantity))
                item.Listing.UpdateStock(quantity);
            cart.RemoveItem(item.ListingId);
        }

        pendingCheckouts.Remove((order.CustomerId, order.RestaurantId));
        if (ReferenceEquals(Cart, cart)) customerApiOrderIds.Add(order.Id);
        return CommitCheckout(cart, order);
    }

    private Order CommitCheckout(Cart cart, Order order)
    {
        var orders = FindRestaurant(order.RestaurantId)!.Orders;
        var existing = orders.FindIndex(candidate => candidate.Id == order.Id);
        if (existing < 0) orders.Add(order);
        else orders[existing] = order;
        if (ReferenceEquals(Cart, cart)) LastOrder = order;
        Persist();
        return order;
    }

    internal void Restore(List<Restaurant> restaurants, List<Cart> carts, List<User> users, List<SurplusListing> listings)
    {
        Restaurants = restaurants; Restaurant = restaurants[0]; Users = users; Listings = listings;
        Carts = carts; Cart = new Cart(); CurrentUser = null; LastOrder = null;
    }

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
        RequireCustomer();
        EnsureNotCheckingOut();
        if (!listing.Menu.IsActive) throw new InvalidOperationException("Menu ini sudah tidak aktif.");
        Cart.AddItem(listing, quantity); Persist();
    }

    public void ChangeQuantity(Guid listingId, int quantity)
    {
        EnsureWritable();
        RequireCustomer();
        EnsureNotCheckingOut();
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
        category = MenuCategories.Normalize(category);
        var restaurant = RequireOwnedRestaurant(existing?.RestaurantId ?? Restaurant.Id);
        var errors = ValidateMenu(existing, name, description, normalPrice, weight);
        if (errors.Count > 0) throw new InvalidOperationException(errors.First().Value);
        var menu = existing ?? new Menu { RestaurantId = restaurant.Id, Restaurant = restaurant };
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
            if (existing is null) restaurant.Menus.Add(menu);
            Persist(); return menu;
        }
        catch
        {
            if (existing is null) restaurant.Menus.Remove(menu);
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
        var owner = RequireOwnedRestaurant(menu.RestaurantId);
        var index = owner.Menus.FindIndex(m => m.Id == menu.Id);
        if (index < 0) throw new InvalidOperationException("Menu tidak ditemukan.");
        var stored = owner.Menus[index];
        if (stored.SurplusListings.Count > 0 || Listings.Any(l => l.MenuId == stored.Id || l.Menu.Id == stored.Id) ||
            Carts.SelectMany(c => c.Items).Any(i => i.Listing.MenuId == stored.Id || i.Listing.Menu.Id == stored.Id) ||
            AllOrders.Any(o => o.Items.Any(i => i.Listing.MenuId == stored.Id || i.Listing.Menu.Id == stored.Id)) ||
            owner.Menus.SelectMany(m => m.ProductionRecords).Any(r => r.MenuId == stored.Id || r.Menu?.Id == stored.Id) ||
            stored.ProductionRecords.Count > 0)
            throw new InvalidOperationException("Menu tidak dapat dihapus karena masih digunakan oleh penawaran surplus, keranjang, pesanan, atau catatan produksi. Gunakan Nonaktifkan agar riwayat tetap tersimpan.");
        owner.Menus.RemoveAt(index);
        try { Persist(); }
        catch { owner.Menus.Insert(index, stored); throw; }
    }

    public void ToggleMenu(Menu menu)
    {
        EnsureWritable();
        RequireOwnedRestaurant(menu.RestaurantId);
        if (menu.IsActive) menu.Deactivate(); else menu.Activate();
        Persist();
    }

    public SurplusListing SaveListing(SurplusListing? existing, Menu menu, decimal price, int quantity, DateTime start, DateTime end)
    {
        EnsureWritable();
        var restaurant = RequireOwnedRestaurant(menu.RestaurantId);
        if (existing is not null) RequireOwnedRestaurant(existing.Menu.RestaurantId);
        if (existing is not null && existing.MenuId != menu.Id)
            throw new InvalidOperationException("Menu pada penawaran yang sudah dibuat tidak dapat diganti.");
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
        if (existing is not null && AllOrders.Any(o => o.Status is not (OrderStatus.Completed or OrderStatus.Cancelled) && o.Items.Any(i => i.ListingId == existing.Id)) &&
            (ToMinute(existing.PickupStart.ToLocalTime()) != ToMinute(start) || ToMinute(existing.PickupEnd.ToLocalTime()) != ToMinute(end)))
            throw new InvalidOperationException("Jadwal tidak dapat diubah saat masih ada pesanan aktif.");
        var listing = existing ?? new SurplusListing { Menu = menu, MenuId = menu.Id };
        if (UsesSharedStock)
        {
            var proposed = new SurplusListing
            {
                Id = listing.Id, Menu = menu, MenuId = menu.Id, RescuePrice = price,
                Quantity = quantity, PickupStart = start.ToUniversalTime(), PickupEnd = end.ToUniversalTime()
            };
            new DatabaseService().SaveListing(restaurant, proposed, sharedListingIds.Contains(listing.Id) ? listing.Quantity : null);
            sharedListingIds.Add(listing.Id);
        }
        listing.RescuePrice = price; listing.PickupStart = start.ToUniversalTime(); listing.PickupEnd = end.ToUniversalTime(); listing.UpdateStock(quantity);
        if (existing is null) { Listings.Add(listing); menu.SurplusListings.Add(listing); }
        Persist(); return listing;
    }

    private static DateTime ToMinute(DateTime value) => new(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0);

    private int Allocated(Menu menu, DateOnly date, SurplusListing? except = null) =>
        Listings.Where(l => l.MenuId == menu.Id && l.Id != except?.Id && l.Status == ListingStatus.Active &&
            DateOnly.FromDateTime(l.PickupStart.ToLocalTime()) == date && l.PickupEnd > DateTime.UtcNow).Sum(l => l.Quantity)
        + AllOrders.Where(o => o.Status is not (OrderStatus.Completed or OrderStatus.Cancelled))
            .SelectMany(o => o.Items).Where(i => i.Listing.MenuId == menu.Id && DateOnly.FromDateTime(i.Listing.PickupStart.ToLocalTime()) == date).Sum(i => i.Quantity);

    public void CancelListing(SurplusListing listing)
    {
        EnsureWritable();
        RequireOwnedRestaurant(listing.Menu.RestaurantId);
        if (UsesSharedStock && sharedListingIds.Contains(listing.Id)) new DatabaseService().CloseListing(listing.Id);
        listing.Cancel();
        Persist();
    }

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
        RequireOwnedRestaurant(menu.RestaurantId);
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
        RequireOwnedRestaurant(order.RestaurantId);
        if (supabaseSession is null) EnsurePickupWindowOpen(order);
        if (preparationMinutes is < 1 or > 240) throw new InvalidOperationException("Waktu persiapan harus antara 1 dan 240 menit.");
        if (order.Status != OrderStatus.Reserved)
            throw new InvalidOperationException("Hanya pesanan yang menunggu konfirmasi yang dapat dikonfirmasi.");
        EnsureNotCheckingOut();
        if (supabaseSession is { } session)
            SupabaseOrderWriter.UpdateStatusAsync(session, order.Id, OrderStatus.Confirmed, preparationMinutes).GetAwaiter().GetResult();
        else if (UsesSharedStock && order.Items.Any(item => sharedListingIds.Contains(item.ListingId)))
            new DatabaseService().ConfirmOrder(order.Id, order.RestaurantId, preparationMinutes);
        order.Confirm(); order.SetPreparationTime(TimeSpan.FromMinutes(preparationMinutes)); Persist();
    }

    public void ReadyOrder(Order order)
    {
        EnsureWritable();
        EnsureNotCheckingOut();
        RequireOwnedRestaurant(order.RestaurantId);
        if (supabaseSession is null) EnsurePickupWindowOpen(order);
        if (order.Status != OrderStatus.Confirmed)
            throw new InvalidOperationException("Pesanan harus dikonfirmasi sebelum ditandai siap diambil.");
        if (supabaseSession is { } session)
            SupabaseOrderWriter.UpdateStatusAsync(session, order.Id, OrderStatus.ReadyForPickup).GetAwaiter().GetResult();
        else if (UsesSharedStock && order.Items.Any(item => sharedListingIds.Contains(item.ListingId)))
            new DatabaseService().UpdateOrderStatus(order, OrderStatus.ReadyForPickup);
        order.MarkReady();
        Persist();
    }

    public void CancelOrder(Order order) => CancelOrder(order, automatic: false);

    internal void ExpireOverdueOrders(DateTime now)
    {
        if (checkingOut || StorageError is not null) return;
        if (expirationNeedsSave)
        {
            Persist();
            expirationNeedsSave = false;
        }

        foreach (var order in AllOrders.Where(order => order.IsPastPickupWindow(now)))
            CancelOrder(order, automatic: true);
    }

    private void EnsurePickupWindowOpen(Order order)
    {
        if (!order.IsPastPickupWindow(DateTime.UtcNow)) return;
        CancelOrder(order, automatic: true);
        throw new InvalidOperationException("Jadwal pengambilan sudah berakhir. Pesanan dibatalkan.");
    }

    private void CancelOrder(Order order, bool automatic)
    {
        EnsureWritable();
        EnsureNotCheckingOut();
        if (!automatic)
        {
            if (CurrentUser is Customer customer)
            {
                if (order.CustomerId != customer.Id) throw new InvalidOperationException("Pesanan ini bukan milik akun Anda.");
            }
            else RequireOwnedRestaurant(order.RestaurantId);
        }
        if (order.Status is OrderStatus.Completed or OrderStatus.Cancelled)
            throw new InvalidOperationException("Pesanan sudah selesai atau dibatalkan.");
        if (!automatic && supabaseSession is { } session)
        {
            var quantities = SupabaseOrderWriter.CancelOrderAsync(session, order.Id).GetAwaiter().GetResult();
            order.Cancel();
            foreach (var item in order.Items)
                if (quantities.TryGetValue(item.ListingId, out var quantity))
                    item.Listing.UpdateStock(quantity);
            Persist();
            return;
        }

        var restoreStock = true;
        if (UsesSharedStock)
        {
            RefreshSharedListings();
            if (order.Items.Any(item => sharedListingIds.Contains(item.ListingId)))
            {
                restoreStock = new DatabaseService().CancelOrder(order.Id, allowAlreadyCancelled: automatic);
                if (!restoreStock) RefreshSharedListings();
            }
        }
        order.Cancel();
        if (restoreStock)
        {
            var stock = new StockService();
            foreach (var item in order.Items) stock.RestoreStock(item.Listing, item.Quantity);
        }

        if (automatic) expirationNeedsSave = true;
        Persist();
        expirationNeedsSave = false;
    }

    public void CompletePickup(Order order, string code)
    {
        EnsureWritable();
        EnsureNotCheckingOut();
        RequireOwnedRestaurant(order.RestaurantId);
        if (supabaseSession is null) EnsurePickupWindowOpen(order);
        if (order.Status != OrderStatus.ReadyForPickup) throw new InvalidOperationException("Pesanan belum siap diambil atau sudah selesai.");
        var verification = new PickupVerification { Order = order, OrderId = order.Id, EnteredCode = code.Trim().ToUpperInvariant() };
        if (supabaseSession is null && !verification.VerifyCode())
            throw new InvalidOperationException("Kode pengambilan tidak cocok. Periksa kode dari pelanggan.");
        var changes = order.Items.GroupBy(i => (i.Listing.Menu, DateOnly.FromDateTime(i.Listing.PickupStart.ToLocalTime())))
            .Select(g => (Record: g.Key.Menu.ProductionRecords.SingleOrDefault(r => r.RecordDate == g.Key.Item2)
                ?? throw new InvalidOperationException("Catatan produksi untuk pesanan ini tidak ditemukan."), Quantity: g.Sum(i => i.Quantity))).ToList();
        if (changes.Any(c => c.Record.RescuedQuantity + c.Quantity > c.Record.CalculateSurplus()))
            throw new InvalidOperationException("Porsi terselamatkan melebihi catatan surplus. Periksa catatan produksi.");
        if (supabaseSession is { } session)
        {
            var completedAt = SupabaseOrderWriter.UpdateStatusAsync(session, order.Id, OrderStatus.Completed,
                pickupCode: verification.EnteredCode).GetAwaiter().GetResult();
            order.Complete(completedAt);
        }
        else
        {
            if (UsesSharedStock && order.Items.Any(item => sharedListingIds.Contains(item.ListingId)))
                new DatabaseService().UpdateOrderStatus(order, OrderStatus.Completed, verification.VerifiedAt);
            verification.CompletePickup();
        }
        foreach (var change in changes) change.Record.UpdateRescuedQuantity(change.Record.RescuedQuantity + change.Quantity);
        Persist();
    }

    public static string Status(Order order) => order.Status switch
    {
        OrderStatus.Reserved => "Menunggu Konfirmasi", OrderStatus.Confirmed => "Diproses",
        OrderStatus.ReadyForPickup => "Siap Diambil", OrderStatus.Completed => "Selesai", _ => "Dibatalkan"
    };
}
