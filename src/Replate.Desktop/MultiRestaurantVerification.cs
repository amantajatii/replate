using System.Text.Json;
using System.Text.Json.Nodes;
using Replate.Domain.Entities;
using Replate.Domain.Enums;

namespace Replate.Desktop;

internal static class MultiRestaurantVerification
{
    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "replate-multi-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        // Version 1 migration
        var fixture = Version1Fixture();
        var path = Path.Combine(directory, "state.json");
        var original = JsonSerializer.Serialize(fixture.Json);
        File.WriteAllText(path, original);
        var migrated = new DemoSession(path);
        Ensure(migrated.StorageError is null, "Version 1 file loads: " + migrated.StorageError);
        Ensure(migrated.Restaurants.Count == 1 && migrated.Restaurant.Id == fixture.RestaurantId &&
            migrated.Restaurant.Name == "Dapur Lama" && migrated.Restaurant.Address == "Jl. Lama No. 1", "Single restaurant migrates");
        var migratedMenu = migrated.Restaurant.Menus.Single();
        Ensure(migratedMenu.Id == fixture.MenuId && migratedMenu.RestaurantId == fixture.RestaurantId &&
            migratedMenu.Restaurant == migrated.Restaurant, "Menus migrate with restaurant id");
        Ensure(migrated.Listings.Single().Menu == migratedMenu && migratedMenu.ProductionRecords.Single().RescuedQuantity == 2, "Listings and records migrate");
        var migratedOrder = migrated.Restaurant.Orders.Single();
        Ensure(migratedOrder.Id == fixture.OrderId && migratedOrder.RestaurantId == fixture.RestaurantId &&
            migrated.CustomerOrders.Single() == migratedOrder, "Orders migrate");
        Ensure(migrated.Carts.Count == 1 && migrated.Cart == migrated.Carts[0] && migrated.Cart.Id == fixture.CartId &&
            migrated.Cart.RestaurantId == fixture.RestaurantId && migrated.Cart.Items.Single().Quantity == 1, "Single cart migrates to carts");
        Ensure(migrated.Users.Count == 0, "Migrated users are empty");

        // Version 1 backup
        var backup = path + ".v1.bak";
        Ensure(File.Exists(backup) && File.ReadAllText(backup) == original, "Version 1 backup is created");
        Ensure(File.ReadAllText(path) == original, "Loading does not rewrite the original file");
        migrated.Persist();
        Ensure(Version(path) == 2 && File.ReadAllText(backup) == original, "Save writes version 2 and keeps backup");
        var reloaded = new DemoSession(path);
        Ensure(reloaded.StorageError is null && reloaded.Restaurant.Orders.Single().Id == fixture.OrderId &&
            reloaded.Cart.Items.Single().ListingId == fixture.ListingId, "Migrated data survives version 2 reload");
        Ensure(File.ReadAllText(backup) == original, "Version 2 load leaves backup untouched");

        var keptPath = Path.Combine(directory, "kept.json");
        File.WriteAllText(keptPath, original);
        File.WriteAllText(keptPath + ".v1.bak", "backup lama");
        Ensure(new DemoSession(keptPath).StorageError is null && File.ReadAllText(keptPath + ".v1.bak") == "backup lama",
            "Existing backup is not overwritten");

        var unsupportedPath = Path.Combine(directory, "future.json");
        var unsupported = JsonNode.Parse(original)!; unsupported["Version"] = 3;
        File.WriteAllText(unsupportedPath, unsupported.ToJsonString());
        var future = new DemoSession(unsupportedPath);
        ExpectFailure(future.Persist);
        Ensure(future.StorageError is not null && File.ReadAllText(unsupportedPath) == unsupported.ToJsonString() &&
            !File.Exists(unsupportedPath + ".v1.bak"), "Unsupported version stays read-only");

        var brokenPath = Path.Combine(directory, "broken.json");
        var broken = JsonNode.Parse(original)!; broken["Listings"]![0]!["MenuId"] = Guid.NewGuid();
        File.WriteAllText(brokenPath, broken.ToJsonString());
        var brokenSession = new DemoSession(brokenPath);
        ExpectFailure(brokenSession.Persist);
        Ensure(brokenSession.StorageError is not null && File.ReadAllText(brokenPath) == broken.ToJsonString() &&
            File.ReadAllText(brokenPath + ".v1.bak") == broken.ToJsonString(), "Failed migration preserves original and backup");

        // Cross-restaurant search
        var seededPath = Path.Combine(directory, "seeded.json");
        var session = new DemoSession(seededPath);
        Ensure(session.Restaurants.Count == 3 && session.Restaurants.Select(r => r.Name).Distinct().Count() == 3 &&
            session.Restaurant == session.Restaurants[0] && session.Restaurant.Name == "Dapur Bumi", "Three seeded restaurants, Dapur Bumi active");
        Ensure(session.Restaurants.All(r => r.Menus.Count > 0 && r.Orders.Count > 0 && r.Menus.All(m => m.ProductionRecords.Count > 0)),
            "Each restaurant has menus, production, and orders");
        var bumi = session.Restaurants[0]; var sambal = session.Restaurants[1]; var pagi = session.Restaurants[2];
        Ensure(session.Search("", "Semua").Select(l => l.Menu.RestaurantId).Distinct().Count() == 3, "Marketplace lists all restaurants");
        Ensure(session.Search("sambal ijo", "Semua").Count() == sambal.Menus.Count &&
            session.Search("sambal ijo", "Semua").All(l => l.Menu.RestaurantId == sambal.Id), "Search by another restaurant name");
        session.SelectRestaurant(pagi.Id);
        Ensure(session.Search("dapur bumi", "Semua").Count() == bumi.Menus.Count, "Search ignores active restaurant");
        Ensure(session.Search("gado", "Vegetarian").Single().Menu.RestaurantId == sambal.Id, "Search by menu name across restaurants");
        Ensure(session.RestaurantListings.All(l => l.Menu.RestaurantId == pagi.Id) && session.RestaurantListings.Count() == pagi.Menus.Count,
            "Restaurant area lists only the active restaurant's offers");
        ExpectFailure(() => session.SelectRestaurant(Guid.NewGuid()));

        // Checkout restaurant
        session.SelectRestaurant(bumi.Id);
        var pagiListing = session.Listings.First(l => l.Menu.RestaurantId == pagi.Id);
        var bumiOrders = bumi.Orders.Count; var pagiOrders = pagi.Orders.Count;
        session.AddToCart(pagiListing, 1);
        var order = session.Checkout();
        Ensure(order.RestaurantId == pagi.Id && pagi.Orders.Contains(order) && pagi.Orders.Count == pagiOrders + 1 &&
            bumi.Orders.Count == bumiOrders, "Checkout adds order to the cart restaurant");
        Ensure(session.CustomerOrders.Single() == order, "Customer orders collected across restaurants");
        session.AddToCart(session.Listings.First(l => l.Menu.RestaurantId == sambal.Id), 1);
        var sambalOrder = session.Checkout();
        Ensure(sambal.Orders.Contains(sambalOrder) && session.CustomerOrders.Count() == 2, "Customer orders span restaurants");
        session = new DemoSession(seededPath);
        Ensure(session.FindRestaurant(pagi.Id)!.Orders.Any(o => o.Id == order.Id) &&
            session.FindRestaurant(sambal.Id)!.Orders.Any(o => o.Id == sambalOrder.Id) &&
            session.Restaurants[0].Orders.All(o => o.Id != order.Id && o.Id != sambalOrder.Id), "Orders reload into their restaurants");

        var orphanMenu = new Menu { Name = "Menu tanpa restoran", NormalPrice = 20000, WeightPerPortion = .3m, RestaurantId = Guid.NewGuid() };
        var orphanListing = new SurplusListing { Menu = orphanMenu, MenuId = orphanMenu.Id, RescuePrice = 10000, Quantity = 3,
            PickupStart = DateTime.UtcNow.AddHours(-1), PickupEnd = DateTime.UtcNow.AddHours(1) };
        var orphanSession = new DemoSession();
        orphanSession.Cart.AddItem(orphanListing, 1);
        var message = "";
        try { orphanSession.Checkout(); } catch (InvalidOperationException e) { message = e.Message; }
        Ensure(message.StartsWith("Restoran untuk keranjang ini tidak ditemukan", StringComparison.Ordinal) &&
            orphanListing.Quantity == 3 && orphanSession.Cart.Items.Count == 1 && orphanSession.LastOrder is null,
            "Unknown cart restaurant is rejected before stock changes");

        // Cart restaurant rule
        var sambalListing = session.Listings.First(l => l.Menu.RestaurantId == sambal.Id && l.IsAvailable(DateTime.UtcNow));
        var bumiListing = session.Listings.First(l => l.Menu.RestaurantId == bumi.Id && l.IsAvailable(DateTime.UtcNow));
        session.AddToCart(sambalListing, 1);
        ExpectFailure(() => session.AddToCart(bumiListing, 1));
        Ensure(session.Cart.Items.Single().ListingId == sambalListing.Id && session.Cart.RestaurantId == sambal.Id, "Cart rejects another restaurant");
        session = new DemoSession(seededPath);
        Ensure(session.Cart.Items.Single().ListingId == sambalListing.Id && session.Cart.RestaurantId == sambal.Id, "Cart restaurant survives reload");

        // Version 2 round trip
        session.Users.Add(new Customer { Name = "Sari", Email = "sari@example.com", PasswordHash = "hash-pelanggan" });
        session.Users.Add(new RestaurantOwner { Name = "Bima", Email = "bima@example.com", PasswordHash = "hash-pemilik" });
        session.SelectRestaurant(sambal.Id);
        var sambalMenu = session.SaveMenu(null, "Es teh sereh", "Teh dingin dengan sereh.", "Vegetarian", 8000, .2m);
        var savedText = File.ReadAllText(seededPath);
        Ensure(Version(seededPath) == 2, "Save writes version 2");
        var roundTrip = new DemoSession(seededPath);
        Ensure(roundTrip.StorageError is null, "Version 2 file loads: " + roundTrip.StorageError);
        Ensure(roundTrip.Restaurants.Select(r => (r.Id, r.Name, r.Address)).SequenceEqual(session.Restaurants.Select(r => (r.Id, r.Name, r.Address))),
            "Restaurants round trip");
        Ensure(roundTrip.Restaurants.SelectMany(r => r.Menus).All(m => m.Restaurant!.Id == m.RestaurantId) &&
            roundTrip.FindRestaurant(sambal.Id)!.Menus.Any(m => m.Id == sambalMenu.Id), "Menus round trip into their restaurants");
        Ensure(roundTrip.Users.Select(u => (u.Id, u.Name, u.Email, u.PasswordHash, u.Role))
            .SequenceEqual(session.Users.Select(u => (u.Id, u.Name, u.Email, u.PasswordHash, u.Role))), "Users round trip");
        Ensure(roundTrip.AllOrders.Count() == session.AllOrders.Count() && roundTrip.Listings.Count == session.Listings.Count &&
            roundTrip.Carts.Count == 1 && roundTrip.Cart.Id == session.Cart.Id, "Orders, listings, and carts round trip");
        var copyPath = Path.Combine(directory, "copy.json");
        SessionStore.Save(roundTrip, copyPath);
        Ensure(File.ReadAllText(copyPath) == savedText, "Version 2 save and load are lossless");
        Ensure(!File.Exists(seededPath + ".v1.bak"), "Version 2 files are not backed up as version 1");

        Directory.Delete(directory, true);
        Console.WriteLine("All Replate multi-restaurant checks passed.");
    }

    private sealed record Fixture(object Json, Guid RestaurantId, Guid MenuId, Guid ListingId, Guid OrderId, Guid CartId);

    private static Fixture Version1Fixture()
    {
        Guid restaurantId = Guid.NewGuid(), menuId = Guid.NewGuid(), listingId = Guid.NewGuid(),
            orderId = Guid.NewGuid(), cartId = Guid.NewGuid(), customerId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var json = new
        {
            Version = 1,
            Restaurant = new { Id = restaurantId, Name = "Dapur Lama", Address = "Jl. Lama No. 1" },
            Menus = new[] { new { Id = menuId, Name = "Nasi goreng", Description = "Nasi goreng kampung.", Category = "Makanan utama",
                NormalPrice = 30000m, WeightPerPortion = .3m, IsActive = true, ImageUrl = "" } },
            Listings = new[] { new { Id = listingId, MenuId = menuId, RescuePrice = 15000m, Quantity = 5,
                PickupStart = now.AddHours(-1), PickupEnd = now.AddHours(3), Status = ListingStatus.Active } },
            Records = new[] { new { Id = Guid.NewGuid(), MenuId = menuId, RecordDate = DateOnly.FromDateTime(DateTime.Today),
                ProducedQuantity = 20, RegularSoldQuantity = 10, RescuedQuantity = 2 } },
            Orders = new[] { new { Id = orderId, CustomerId = customerId, RestaurantId = restaurantId, PickupCode = "ABC234",
                TotalPrice = 30000m, Status = OrderStatus.Reserved, PreparationDuration = TimeSpan.Zero,
                EstimatedReadyTime = DateTime.MinValue, OrderedAt = now.AddMinutes(-10), CompletedAt = (DateTime?)null,
                Items = new[] { new { ListingId = listingId, MenuName = "Nasi goreng", Quantity = 2, UnitPrice = 15000m } } } },
            Cart = new { Id = cartId, CustomerId = customerId, RestaurantId = restaurantId,
                Items = new[] { new { ListingId = listingId, Quantity = 1, UnitPrice = 15000m } } }
        };
        return new Fixture(json, restaurantId, menuId, listingId, orderId, cartId);
    }

    private static int Version(string path) => JsonNode.Parse(File.ReadAllText(path))!["Version"]!.GetValue<int>();

    private static void Ensure(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }

    private static void ExpectFailure(Action action)
    {
        try { action(); } catch (InvalidOperationException) { return; }
        throw new InvalidOperationException("Expected validation failure.");
    }
}
