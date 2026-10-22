using System.Text.Json;
using Replate.Domain.Entities;

namespace Replate.Desktop;

// Acyclic snapshots keep UI persistence independent of domain navigation graphs.
internal static class SessionStore
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PreferredObjectCreationHandling = System.Text.Json.Serialization.JsonObjectCreationHandling.Populate
    };
    internal sealed record Snapshot(int Version, RestaurantState Restaurant, List<MenuState> Menus,
        List<ListingState> Listings, List<RecordState> Records, List<OrderState> Orders, CartState Cart);
    internal sealed record RestaurantState(Guid Id, string Name, string Address);
    internal sealed record MenuState(Guid Id, string Name, string Description, string Category,
        decimal NormalPrice, decimal WeightPerPortion, bool IsActive);
    internal sealed record ListingState(Guid Id, Guid MenuId, decimal RescuePrice, int Quantity,
        DateTime PickupStart, DateTime PickupEnd, Replate.Domain.Enums.ListingStatus Status);
    internal sealed record RecordState(Guid Id, Guid MenuId, DateOnly RecordDate, int ProducedQuantity,
        int RegularSoldQuantity, int RescuedQuantity);
    internal sealed record ItemState(Guid ListingId, string MenuName, int Quantity, decimal UnitPrice);
    internal sealed record OrderState(Guid Id, Guid CustomerId, Guid RestaurantId, string PickupCode,
        decimal TotalPrice, Replate.Domain.Enums.OrderStatus Status, TimeSpan PreparationDuration,
        DateTime EstimatedReadyTime, DateTime OrderedAt, DateTime? CompletedAt, List<ItemState> Items);
    internal sealed record CartState(Guid Id, Guid CustomerId, Guid RestaurantId, List<CartItemState> Items);
    internal sealed record CartItemState(Guid ListingId, int Quantity, decimal UnitPrice);

    internal static void Save(DemoSession session, string path)
    {
        var state = new Snapshot(1,
            new(session.Restaurant.Id, session.Restaurant.Name, session.Restaurant.Address),
            session.Restaurant.Menus.Select(m => new MenuState(m.Id, m.Name, m.Description, m.Category,
                m.NormalPrice, m.WeightPerPortion, m.IsActive)).ToList(),
            session.Listings.Select(l => new ListingState(l.Id, l.MenuId, l.RescuePrice, l.Quantity,
                l.PickupStart, l.PickupEnd, l.Status)).ToList(),
            session.Restaurant.Menus.SelectMany(m => m.ProductionRecords).Select(r =>
                new RecordState(r.Id, r.MenuId, r.RecordDate, r.ProducedQuantity, r.RegularSoldQuantity, r.RescuedQuantity)).ToList(),
            session.Restaurant.Orders.Select(o => new OrderState(o.Id, o.CustomerId, o.RestaurantId, o.PickupCode,
                o.TotalPrice, o.Status, o.PreparationDuration, o.EstimatedReadyTime, o.OrderedAt, o.CompletedAt,
                o.Items.Select(i => new ItemState(i.ListingId, i.MenuName, i.Quantity, i.UnitPrice)).ToList())).ToList(),
            new(session.Cart.Id, session.Cart.CustomerId, session.Cart.RestaurantId,
                session.Cart.Items.Select(i => new CartItemState(i.ListingId, i.Quantity, i.UnitPrice)).ToList()));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(state, Json));
        File.Move(temporary, path, true);
    }

    internal static void Load(DemoSession session, string path)
    {
        var state = JsonSerializer.Deserialize<Snapshot>(File.ReadAllText(path), Json)
            ?? throw new InvalidDataException("Berkas data kosong.");
        if (state.Version != 1) throw new InvalidDataException("Versi berkas data tidak didukung.");
        // Materialize JSON snapshots into entities, then rebuild references in one pass.
        var restaurant = new Restaurant { Id = state.Restaurant.Id, Name = state.Restaurant.Name, Address = state.Restaurant.Address };
        var menus = state.Menus.Select(Convert<Menu>).ToDictionary(m => m.Id);
        foreach (var menu in menus.Values)
        {
            menu.Restaurant = restaurant; menu.RestaurantId = restaurant.Id; restaurant.Menus.Add(menu);
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
            restaurant.Orders.Add(order);
        }
        var cart = Convert<Cart>(state.Cart);
        foreach (var item in cart.Items) { item.Listing = listings[item.ListingId]; item.CartId = cart.Id; }
        if (menus.Values.Any(m => string.IsNullOrWhiteSpace(m.Name) || m.NormalPrice <= 0 || m.WeightPerPortion <= 0) ||
            listings.Values.Any(l => l.Quantity < 0 || l.RescuePrice <= 0 || l.PickupEnd <= l.PickupStart || !Enum.IsDefined(l.Status)) ||
            restaurant.Menus.SelectMany(m => m.ProductionRecords).Any(r => r.ProducedQuantity < 0 || r.RegularSoldQuantity < 0 ||
                r.RegularSoldQuantity > r.ProducedQuantity || r.RescuedQuantity < 0 || r.RescuedQuantity > r.CalculateSurplus()) ||
            restaurant.Orders.Any(o => !Enum.IsDefined(o.Status) || o.Items.Count == 0 ||
                o.Items.Any(i => i.Quantity <= 0 || i.UnitPrice <= 0) || o.TotalPrice != o.Items.Sum(i => i.CalculateSubtotal())) ||
            cart.Items.Any(i => i.Quantity <= 0 || i.UnitPrice <= 0))
            throw new InvalidDataException("Berkas data mengandung nilai yang tidak valid.");
        session.Restore(restaurant, cart, listings.Values.ToList());
    }

    private static T Convert<T>(object value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, Json), Json)!;
}
