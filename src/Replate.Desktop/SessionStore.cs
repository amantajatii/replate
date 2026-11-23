using System.Text.Json;
using Replate.Domain.Entities;
using Replate.Domain.Enums;
using Replate.Domain.ValueObjects;
using Replate.Desktop.Data;

namespace Replate.Desktop;

// Acyclic snapshots keep UI persistence independent of domain navigation graphs.
internal static class SessionStore
{
    internal const int CurrentVersion = 2;
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PreferredObjectCreationHandling = System.Text.Json.Serialization.JsonObjectCreationHandling.Populate
    };
    internal sealed record VersionProbe(int Version);
    internal sealed record Snapshot(int Version, List<RestaurantState> Restaurants, List<MenuState> Menus,
        List<ListingState> Listings, List<RecordState> Records, List<OrderState> Orders,
        List<UserState> Users, List<CartState> Carts, Dictionary<Guid, CustomerLocationState>? ExternalCustomerLocations = null);
    internal sealed record SnapshotV1(int Version, RestaurantState Restaurant, List<MenuState> Menus,
        List<ListingState> Listings, List<RecordState> Records, List<OrderState> Orders, CartState Cart);
    internal sealed record RestaurantState(Guid Id, string Name, string Address, Guid OwnerId = default, string Description = "",
        string Phone = "", TimeOnly OpeningTime = default, TimeOnly ClosingTime = default, bool IsImported = false,
        double? Latitude = null, double? Longitude = null);
    internal sealed record MenuState(Guid Id, string Name, string Description, string Category,
        decimal NormalPrice, decimal WeightPerPortion, bool IsActive, string ImageUrl = "", Guid RestaurantId = default);
    internal sealed record ListingState(Guid Id, Guid MenuId, decimal RescuePrice, int Quantity,
        DateTime PickupStart, DateTime PickupEnd, ListingStatus Status);
    internal sealed record RecordState(Guid Id, Guid MenuId, DateOnly RecordDate, int ProducedQuantity,
        int RegularSoldQuantity, int RescuedQuantity);
    internal sealed record ItemState(Guid ListingId, string MenuName, int Quantity, decimal UnitPrice);
    internal sealed record OrderState(Guid Id, Guid CustomerId, Guid RestaurantId, string PickupCode,
        decimal TotalPrice, OrderStatus Status, TimeSpan PreparationDuration,
        DateTime EstimatedReadyTime, DateTime OrderedAt, DateTime? CompletedAt, List<ItemState> Items);
    internal sealed record UserState(Guid Id, string Name, string Email, string PasswordHash, UserRole Role,
        double? Latitude = null, double? Longitude = null, string? LocationLabel = null);
    internal sealed record CustomerLocationState(double Latitude, double Longitude, string Label);
    internal sealed record CartState(Guid Id, Guid CustomerId, Guid RestaurantId, List<CartItemState> Items);
    internal sealed record CartItemState(Guid ListingId, int Quantity, decimal UnitPrice);

    internal static string BackupPath(string path) => path + ".v1.bak";

    internal static void Save(DemoSession session, string path)
    {
        var menus = session.Restaurants.SelectMany(r => r.Menus).ToList();
        var state = new Snapshot(CurrentVersion,
            session.Restaurants.Select(r => new RestaurantState(r.Id, r.Name, r.Address,
                session.ImportedRestaurantIds.Contains(r.Id) ? Guid.Empty : r.OwnerId, r.Description,
                r.Phone, r.OpeningTime, r.ClosingTime, session.ImportedRestaurantIds.Contains(r.Id),
                r.Location?.Latitude, r.Location?.Longitude)).ToList(),
            menus.Select(m => new MenuState(m.Id, m.Name, m.Description, m.Category,
                m.NormalPrice, m.WeightPerPortion, m.IsActive, m.ImageUrl, m.RestaurantId)).ToList(),
            session.Listings.Select(l => new ListingState(l.Id, l.MenuId, l.RescuePrice, l.Quantity,
                l.PickupStart, l.PickupEnd, l.Status)).ToList(),
            menus.SelectMany(m => m.ProductionRecords).Select(r =>
                new RecordState(r.Id, r.MenuId, r.RecordDate, r.ProducedQuantity, r.RegularSoldQuantity, r.RescuedQuantity)).ToList(),
            session.Restaurants.SelectMany(r => r.Orders.Select(o => new OrderState(o.Id, o.CustomerId, r.Id, o.PickupCode,
                o.TotalPrice, o.Status, o.PreparationDuration, o.EstimatedReadyTime, o.OrderedAt, o.CompletedAt,
                o.Items.Select(i => new ItemState(i.ListingId, i.MenuName, i.Quantity, i.UnitPrice)).ToList()))).ToList(),
            session.Users.Select(ToUserState).ToList(),
            session.Carts.Select(c => new CartState(c.Id, c.CustomerId, c.GroupByRestaurant().FirstOrDefault()?.RestaurantId ?? Guid.Empty,
                c.Items.Select(i => new CartItemState(i.ListingId, i.Quantity, i.UnitPrice)).ToList())).ToList(),
            session.ExternalCustomerLocations.Count == 0 ? null : session.ExternalCustomerLocations.ToDictionary(
                pair => pair.Key, pair => new CustomerLocationState(pair.Value.Point.Latitude, pair.Value.Point.Longitude, pair.Value.Label)));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(state, Json));
        File.Move(temporary, path, true);
    }

    internal static void Load(DemoSession session, string path)
    {
        var text = File.ReadAllText(path);
        var probe = JsonSerializer.Deserialize<VersionProbe>(text, Json) ?? throw new InvalidDataException("Berkas data kosong.");
        Snapshot state;
        if (probe.Version == 1)
        {
            BackUpVersion1(path);
            state = Migrate(JsonSerializer.Deserialize<SnapshotV1>(text, Json) ?? throw new InvalidDataException("Berkas data kosong."));
        }
        else if (probe.Version == CurrentVersion)
            state = JsonSerializer.Deserialize<Snapshot>(text, Json) ?? throw new InvalidDataException("Berkas data kosong.");
        else throw new InvalidDataException("Versi berkas data tidak didukung.");

        // Materialize JSON snapshots into entities, then rebuild references in one pass.
        var restaurantList = state.Restaurants.Select(r => new Restaurant { Id = r.Id, Name = r.Name, Address = r.Address,
            Description = r.Description ?? "", Phone = r.Phone ?? "", OpeningTime = r.OpeningTime, ClosingTime = r.ClosingTime }).ToList();
        if (restaurantList.Count == 0) throw new InvalidDataException("Berkas data tidak memuat restoran.");
        var restaurants = restaurantList.ToDictionary(r => r.Id);
        var menus = state.Menus.Select(Convert<Menu>).ToDictionary(m => m.Id);
        foreach (var menu in menus.Values)
        {
            var restaurant = restaurants[menu.RestaurantId];
            menu.Category = MenuCategories.Normalize(menu.Category);
            menu.Restaurant = restaurant; restaurant.Menus.Add(menu);
        }
        var listings = state.Listings.Select(Convert<SurplusListing>).ToDictionary(l => l.Id);
        foreach (var listing in listings.Values)
        {
            listing.Menu = menus[listing.MenuId]; listing.Menu.SurplusListings.Add(listing);
        }
        foreach (var record in state.Records.Select(Convert<ProductionRecord>))
        {
            record.Menu = menus[record.MenuId]; record.Menu.ProductionRecords.Add(record);
        }
        foreach (var order in state.Orders.Select(Convert<Order>))
        {
            foreach (var item in order.Items) { item.Listing = listings[item.ListingId]; item.OrderId = order.Id; }
            restaurants[order.RestaurantId].Orders.Add(order);
        }
        var carts = state.Carts.Select(Convert<Cart>).ToList();
        foreach (var cart in carts)
            foreach (var item in cart.Items) { item.Listing = listings[item.ListingId]; item.CartId = cart.Id; }
        var users = (state.Users ?? []).Select(ToUser).ToList();
        var owners = users.OfType<RestaurantOwner>().ToDictionary(u => u.Id);
        foreach (var (restaurant, snapshot) in restaurantList.Zip(state.Restaurants))
        {
            if (snapshot.Latitude is { } latitude && snapshot.Longitude is { } longitude)
                restaurant.SetLocation(new GeoPoint(latitude, longitude));
            if (snapshot.OwnerId == Guid.Empty) continue;
            if (!owners.TryGetValue(snapshot.OwnerId, out var owner))
                throw new InvalidDataException("Pemilik restoran tidak ditemukan dalam berkas data.");
            restaurant.Owner = owner;
            restaurant.OwnerId = owner.Id;
            owner.Restaurants.Add(restaurant);
        }
        var orders = restaurantList.SelectMany(r => r.Orders).ToList();
        if (menus.Values.Any(m => string.IsNullOrWhiteSpace(m.Name) || m.NormalPrice <= 0 || m.WeightPerPortion <= 0) ||
            listings.Values.Any(l => l.Quantity < 0 || l.RescuePrice <= 0 || l.PickupEnd <= l.PickupStart || !Enum.IsDefined(l.Status)) ||
            menus.Values.SelectMany(m => m.ProductionRecords).Any(r => r.ProducedQuantity < 0 || r.RegularSoldQuantity < 0 ||
                r.RegularSoldQuantity > r.ProducedQuantity || r.RescuedQuantity < 0 || r.RescuedQuantity > r.CalculateSurplus()) ||
            orders.Any(o => !Enum.IsDefined(o.Status) || o.Items.Count == 0 ||
                o.Items.Any(i => i.Quantity <= 0 || i.UnitPrice <= 0) || o.TotalPrice != o.Items.Sum(i => i.CalculateSubtotal())) ||
            carts.Any(c => c.Items.Any(i => i.Quantity <= 0 || i.UnitPrice <= 0)) ||
            users.Select(u => u.Id).Distinct().Count() != users.Count ||
            users.Any(u => string.IsNullOrWhiteSpace(u.Email) || string.IsNullOrWhiteSpace(u.PasswordHash)) ||
            users.Select(u => Replate.Domain.Services.AccountService.NormalizeEmail(u.Email)).Distinct().Count() != users.Count)
            throw new InvalidDataException("Berkas data mengandung nilai yang tidak valid.");
        var externalCustomerLocations = (state.ExternalCustomerLocations ?? []).ToDictionary(pair => pair.Key, pair =>
        {
            var location = pair.Value;
            if (string.IsNullOrWhiteSpace(location.Label)) throw new InvalidDataException("Label lokasi wajib diisi.");
            return new DetectedLocation(new GeoPoint(location.Latitude, location.Longitude), location.Label);
        });
        session.Restore(restaurantList, carts, users, listings.Values.ToList());
        session.ImportedRestaurantIds.UnionWith(state.Restaurants.Where(r => r.IsImported).Select(r => r.Id));
        foreach (var (id, location) in externalCustomerLocations) session.ExternalCustomerLocations.Add(id, location);
    }

    // Version 1 migration
    private static void BackUpVersion1(string path)
    {
        var backup = BackupPath(path);
        if (!File.Exists(backup)) File.Copy(path, backup, false);
    }

    private static Snapshot Migrate(SnapshotV1 old)
    {
        var restaurant = old.Restaurant ?? throw new InvalidDataException("Berkas data tidak memuat restoran.");
        var cart = old.Cart ?? throw new InvalidDataException("Berkas data tidak memuat keranjang.");
        return new Snapshot(CurrentVersion, [restaurant],
            old.Menus.Select(m => m with { RestaurantId = restaurant.Id }).ToList(),
            old.Listings, old.Records,
            old.Orders.Select(o => o with { RestaurantId = restaurant.Id }).ToList(),
            [],
            [cart with { RestaurantId = cart.Items.Count == 0 ? Guid.Empty : restaurant.Id }]);
    }

    private static UserState ToUserState(User user)
    {
        var customer = user as Customer;
        return new UserState(user.Id, user.Name, user.Email, user.PasswordHash, user.Role,
            customer?.Location?.Latitude, customer?.Location?.Longitude, customer?.LocationLabel);
    }

    private static User ToUser(UserState state)
    {
        if (state.Role == UserRole.RestaurantOwner)
            return new RestaurantOwner { Id = state.Id, Name = state.Name, Email = state.Email, PasswordHash = state.PasswordHash };
        if (state.Role != UserRole.Customer)
            throw new InvalidDataException("Peran pengguna tidak dikenal.");

        var customer = new Customer { Id = state.Id, Name = state.Name, Email = state.Email, PasswordHash = state.PasswordHash };
        if (state.Latitude is { } latitude && state.Longitude is { } longitude && state.LocationLabel is { } label)
            customer.SetLocation(new GeoPoint(latitude, longitude), label);
        return customer;
    }

    private static T Convert<T>(object value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, Json), Json)!;
}
