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
        Ensure(migrated.Users.Count == 2 && migrated.Restaurant.Owner is { } migratedOwner &&
            migratedOwner.Email == "dapur-lama@replate.demo" && migrated.Restaurant.OwnerId == migratedOwner.Id &&
            migrated.Users.OfType<Customer>().Single().Email == DemoAccounts.CustomerEmail, "Migrated data receives demo accounts");
        migrated.AsCustomer();
        Ensure(migrated.CurrentUser!.Id == fixture.CustomerId, "Demo customer adopts the migrated cart owner");
        Ensure(migratedOrder.Id == fixture.OrderId && migratedOrder.RestaurantId == fixture.RestaurantId &&
            migrated.CustomerOrders.Single() == migratedOrder, "Orders migrate");
        Ensure(migrated.Carts.Count == 1 && migrated.Cart == migrated.Carts[0] && migrated.Cart.Id == fixture.CartId &&
            migrated.Cart.GroupByRestaurant().Single().RestaurantId == fixture.RestaurantId && migrated.Cart.Items.Single().Quantity == 1, "Single cart migrates to carts");
        migrated.SignIn("dapur-lama@replate.demo", DemoAccounts.Password);
        Ensure(migrated.Restaurant.Id == fixture.RestaurantId, "Migrated demo owner manages the migrated restaurant");

        // Version 1 backup
        var backup = path + ".v1.bak";
        Ensure(File.Exists(backup) && File.ReadAllText(backup) == original, "Version 1 backup is created");
        Ensure(File.ReadAllText(path) == original, "Loading does not rewrite the original file");
        migrated.Persist();
        Ensure(Version(path) == 2 && File.ReadAllText(backup) == original, "Save writes version 2 and keeps backup");
        var reloaded = new DemoSession(path);
        Ensure(reloaded.StorageError is null && reloaded.Users.Count == 2 &&
            reloaded.Users.Select(u => (u.Id, u.Email, u.PasswordHash)).SequenceEqual(migrated.Users.Select(u => (u.Id, u.Email, u.PasswordHash))),
            "Demo accounts are not duplicated after reload");
        reloaded.AsCustomer();
        Ensure(reloaded.Restaurant.Orders.Single().Id == fixture.OrderId &&
            reloaded.Cart.Items.Single().ListingId == fixture.ListingId, "Migrated data survives version 2 reload");
        reloaded.Persist();
        Ensure(new DemoSession(path).Users.Count == 2, "Demo accounts stay single after repeated saves");
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
        session.AsOwner("Roti Pagi Kotabaru");
        session.SelectRestaurant(pagi.Id);
        ExpectFailure(() => session.SelectRestaurant(bumi.Id));
        Ensure(session.Restaurant == pagi, "Owner cannot select another owner's restaurant");
        Ensure(session.Search("dapur bumi", "Semua").Count() == bumi.Menus.Count, "Search ignores active restaurant");
        Ensure(session.Search("gado", MenuCategories.Produce).Single().Menu.RestaurantId == sambal.Id, "Search by menu name across restaurants");
        Ensure(session.RestaurantListings.All(l => l.Menu.RestaurantId == pagi.Id) && session.RestaurantListings.Count() == pagi.Menus.Count,
            "Restaurant area lists only the active restaurant's offers");
        ExpectFailure(() => session.SelectRestaurant(Guid.NewGuid()));

        // Checkout restaurant
        session.AsCustomer();
        var pagiListing = session.Listings.First(l => l.Menu.RestaurantId == pagi.Id);
        var bumiOrders = bumi.Orders.Count; var pagiOrders = pagi.Orders.Count;
        session.AddToCart(pagiListing, 1);
        var order = session.Checkout(pagi.Id);
        Ensure(order.RestaurantId == pagi.Id && pagi.Orders.Contains(order) && pagi.Orders.Count == pagiOrders + 1 &&
            bumi.Orders.Count == bumiOrders, "Checkout adds order to the cart restaurant");
        Ensure(session.CustomerOrders.Single() == order, "Customer orders collected across restaurants");
        session.AddToCart(session.Listings.First(l => l.Menu.RestaurantId == sambal.Id), 1);
        var sambalOrder = session.Checkout(sambal.Id);
        Ensure(sambal.Orders.Contains(sambalOrder) && session.CustomerOrders.Count() == 2, "Customer orders span restaurants");
        session = new DemoSession(seededPath);
        Ensure(session.FindRestaurant(pagi.Id)!.Orders.Any(o => o.Id == order.Id) &&
            session.FindRestaurant(sambal.Id)!.Orders.Any(o => o.Id == sambalOrder.Id) &&
            session.Restaurants[0].Orders.All(o => o.Id != order.Id && o.Id != sambalOrder.Id), "Orders reload into their restaurants");

        var orphanMenu = new Menu { Name = "Menu tanpa restoran", NormalPrice = 20000, WeightPerPortion = .3m, RestaurantId = Guid.NewGuid() };
        var orphanListing = new SurplusListing { Menu = orphanMenu, MenuId = orphanMenu.Id, RescuePrice = 10000, Quantity = 3,
            PickupStart = DateTime.UtcNow.AddHours(-1), PickupEnd = DateTime.UtcNow.AddHours(1) };
        var orphanSession = new DemoSession().AsCustomer();
        orphanSession.Cart.AddItem(orphanListing, 1);
        var message = "";
        try { orphanSession.Checkout(orphanMenu.RestaurantId); } catch (InvalidOperationException e) { message = e.Message; }
        Ensure(message.StartsWith("Restoran untuk keranjang ini tidak ditemukan", StringComparison.Ordinal) &&
            orphanListing.Quantity == 3 && orphanSession.Cart.Items.Count == 1 && orphanSession.LastOrder is null,
            "Unknown cart restaurant is rejected before stock changes");

        // Multi-restaurant cart
        session = new DemoSession(seededPath).AsCustomer();
        bumi = session.FindRestaurant(bumi.Id)!; sambal = session.FindRestaurant(sambal.Id)!;
        var sambalListing = session.Listings.First(l => l.Menu.RestaurantId == sambal.Id && l.IsAvailable(DateTime.UtcNow) && l.Quantity >= 3);
        var bumiListing = session.Listings.First(l => l.Menu.RestaurantId == bumi.Id && l.IsAvailable(DateTime.UtcNow) && l.Quantity >= 3);
        var bumiSecond = session.Listings.First(l => l.Menu.RestaurantId == bumi.Id && l != bumiListing && l.IsAvailable(DateTime.UtcNow) && l.Quantity >= 2);
        session.AddToCart(sambalListing, 2);
        session.AddToCart(bumiListing, 1);
        session.AddToCart(bumiSecond, 1);
        session.AddToCart(bumiListing, 1);
        var groups = session.Cart.GroupByRestaurant();
        Ensure(session.Cart.Items.Count == 3 && session.Cart.CountRestaurants() == 2 &&
            groups.Select(g => g.RestaurantId).SequenceEqual([sambal.Id, bumi.Id]) &&
            groups[0].CalculateSubtotal() == sambalListing.RescuePrice * 2 &&
            groups[1].CalculateSubtotal() == bumiListing.RescuePrice * 2 + bumiSecond.RescuePrice &&
            session.Cart.Items.Single(i => i.ListingId == bumiListing.Id).Quantity == 2, "Cart accepts two restaurants, merges listings and groups by first item");
        ExpectFailure(() => session.AddToCart(sambalListing, sambalListing.Quantity));
        Ensure(session.Cart.Items.Single(i => i.ListingId == sambalListing.Id).Quantity == 2, "Cart quantity stays within stock");

        // Multi-restaurant restart
        var cartBefore = session.Cart.Items.Select(i => (i.ListingId, i.Quantity, i.UnitPrice)).ToList();
        var savedCart = File.ReadAllText(seededPath);
        Ensure(Version(seededPath) == 2 && JsonNode.Parse(savedCart)!["Carts"]!.AsArray()
            .Single(c => c!["CustomerId"]!.GetValue<Guid>() == session.Cart.CustomerId)!["RestaurantId"]!.GetValue<Guid>() == sambal.Id,
            "Multi-restaurant cart is stored in version 2 with the first restaurant");
        session = new DemoSession(seededPath);
        Ensure(session.StorageError is null && File.ReadAllText(seededPath) == savedCart, "Version 2 load does not rewrite the file");
        session.AsCustomer();
        Ensure(session.Cart.Items.Select(i => (i.ListingId, i.Quantity, i.UnitPrice)).SequenceEqual(cartBefore) &&
            session.Cart.CountRestaurants() == 2 && session.Cart.GroupByRestaurant()[0].RestaurantId == sambal.Id, "Multi-restaurant cart survives restart");
        bumi = session.FindRestaurant(bumi.Id)!; sambal = session.FindRestaurant(sambal.Id)!;
        sambalListing = session.Listings.Single(l => l.Id == sambalListing.Id);
        bumiListing = session.Listings.Single(l => l.Id == bumiListing.Id);
        bumiSecond = session.Listings.Single(l => l.Id == bumiSecond.Id);

        // Failed database save
        var stockBefore = (sambalListing.Quantity, bumiListing.Quantity, bumiSecond.Quantity);
        var ordersBefore = session.AllOrders.Count();
        var failing = new TestOrderStore(true);
        session.OrderStore = failing;
        Ensure(FailureMessage(() => session.Checkout(sambal.Id)) == TestOrderStore.Message &&
            FailureMessage(() => session.CheckoutAsync(sambal.Id).GetAwaiter().GetResult()) == TestOrderStore.Message, "Database failure surfaces the existing message");
        Ensure(failing.Calls.Count == 2 && failing.Calls.All(o => o.RestaurantId == sambal.Id), "Only the chosen restaurant order goes to the database");
        var unsaved = failing.Calls.Select(o => o.Id).ToHashSet();
        void Unchanged(string label) => Ensure((sambalListing.Quantity, bumiListing.Quantity, bumiSecond.Quantity) == stockBefore &&
            session.Cart.Items.Select(i => (i.ListingId, i.Quantity, i.UnitPrice)).SequenceEqual(cartBefore) &&
            session.AllOrders.Count() == ordersBefore && session.LastOrder is null && File.ReadAllText(seededPath) == savedCart &&
            !new DemoSession(seededPath).AllOrders.Any(o => unsaved.Contains(o.Id)), label);
        Unchanged("Failed database save leaves stock, cart, and orders unchanged");
        var previous = Environment.GetEnvironmentVariable("REPLATE_DB_CONNECTION_STRING");
        Environment.SetEnvironmentVariable("REPLATE_DB_CONNECTION_STRING", "Host=127.0.0.1;Port=1;Database=replate_db;Username=postgres;Timeout=3");
        try { session.OrderStore = new Data.DatabaseService(); }
        finally { Environment.SetEnvironmentVariable("REPLATE_DB_CONNECTION_STRING", previous); }
        Ensure(FailureMessage(() => session.Checkout(bumi.Id)) == TestOrderStore.Message, "Unavailable PostgreSQL uses the existing message");
        Unchanged("Unavailable PostgreSQL leaves stock, cart, and orders unchanged");
        ExpectFailure(() => session.Checkout(pagi.Id));
        Unchanged("A restaurant outside the cart cannot be ordered");

        // Per-restaurant checkout
        var recording = new TestOrderStore(false);
        session.OrderStore = recording;
        sambalListing.Menu.Deactivate();
        ExpectFailure(() => session.Checkout(sambal.Id));
        Ensure(recording.Calls.Count == 0 && sambalListing.Quantity == stockBefore.Item1 && session.Cart.Items.Count == 3, "An unavailable item blocks its own restaurant");
        var bumiOrderNew = session.Checkout(bumi.Id);
        sambalListing.Menu.Activate(); session.Persist();
        Ensure(session.LastOrder == bumiOrderNew && recording.Calls.Single() == bumiOrderNew,
            "Checkout of one restaurant creates exactly one order and saves it, even with an unavailable item elsewhere");
        Ensure(bumiOrderNew.RestaurantId == bumi.Id && bumiOrderNew.Items.Select(i => (i.ListingId, i.Quantity)).SequenceEqual([(bumiListing.Id, 2), (bumiSecond.Id, 1)]) &&
            bumiOrderNew.TotalPrice == bumiListing.RescuePrice * 2 + bumiSecond.RescuePrice, "Bumi order has its items and total");
        Ensure(bumiOrderNew.CustomerId == session.CurrentUser!.Id && bumiOrderNew.Status == OrderStatus.Reserved && bumiOrderNew.PickupCode.Length == 6,
            "The order has its customer, status, and pickup code");
        Ensure((sambalListing.Quantity, bumiListing.Quantity, bumiSecond.Quantity) == (stockBefore.Item1, stockBefore.Item2 - 2, stockBefore.Item3 - 1),
            "Checkout reduces stock of that restaurant only");
        Ensure(session.Cart.Items.Select(i => (i.ListingId, i.Quantity)).SequenceEqual([(sambalListing.Id, 2)]) && bumi.Orders.Contains(bumiOrderNew) &&
            !sambal.Orders.Contains(bumiOrderNew) && session.AllOrders.Count() == ordersBefore + 1, "Other restaurant items stay in the cart");

        // Remaining cart restart
        session = new DemoSession(seededPath).AsCustomer();
        bumi = session.FindRestaurant(bumi.Id)!; sambal = session.FindRestaurant(sambal.Id)!;
        sambalListing = session.Listings.Single(l => l.Id == sambalListing.Id);
        Ensure(session.Cart.Items.Select(i => (i.ListingId, i.Quantity)).SequenceEqual([(sambalListing.Id, 2)]) && sambalListing.Menu.IsActive &&
            bumi.Orders.Any(o => o.Id == bumiOrderNew.Id) && session.Listings.Single(l => l.Id == bumiListing.Id).Quantity == stockBefore.Item2 - 2,
            "The remaining cart, the order, and the stock survive restart");
        recording = new TestOrderStore(false);
        session.OrderStore = recording;
        var sambalOrderNew = session.Checkout(sambal.Id);
        Ensure(recording.Calls.Single() == sambalOrderNew && sambalOrderNew.RestaurantId == sambal.Id && sambalOrderNew.Id != bumiOrderNew.Id &&
            sambalOrderNew.Items.Single().ListingId == sambalListing.Id && sambalOrderNew.TotalPrice == sambalListing.RescuePrice * 2 &&
            sambalListing.Quantity == stockBefore.Item1 - 2 && session.Cart.Items.Count == 0 && sambal.Orders.Contains(sambalOrderNew),
            "Ordering the last restaurant creates its own order and empties the cart");
        Ensure(new[] { sambalOrderNew.Id, bumiOrderNew.Id }.All(id => session.CustomerOrders.Any(o => o.Id == id)), "Customer sees both orders");

        // Owner order isolation
        session = new DemoSession(seededPath);
        Ensure(session.Carts.All(c => c.Items.Count == 0) && session.FindRestaurant(sambal.Id)!.Orders.Any(o => o.Id == sambalOrderNew.Id) &&
            session.FindRestaurant(bumi.Id)!.Orders.Any(o => o.Id == bumiOrderNew.Id) &&
            session.Listings.Single(l => l.Id == bumiListing.Id).Quantity == stockBefore.Item2 - 2, "Checkout persists orders and stock");
        session.AsOwner("Warung Sambal Ijo");
        Ensure(session.Restaurant.Orders.Any(o => o.Id == sambalOrderNew.Id) && session.Restaurant.Orders.All(o => o.Id != bumiOrderNew.Id),
            "Sambal owner sees only the Sambal order");
        var foreign = session.FindRestaurant(bumi.Id)!.Orders.Single(o => o.Id == bumiOrderNew.Id);
        ExpectFailure(() => session.ConfirmOrder(foreign, 10));
        Ensure(foreign.Status == OrderStatus.Reserved, "Sambal owner cannot process the Bumi order");
        session.AsOwner("Dapur Bumi");
        Ensure(session.Restaurant.Orders.Any(o => o.Id == bumiOrderNew.Id) && session.Restaurant.Orders.All(o => o.Id != sambalOrderNew.Id),
            "Bumi owner sees only the Bumi order");
        session.AsCustomer();
        session.AddToCart(session.Listings.Single(l => l.Id == sambalListing.Id), 1);
        session.AddToCart(session.Listings.Single(l => l.Id == bumiSecond.Id), 1);

        // Version 2 round trip
        session.Register(UserRole.Customer, "Sari", "sari@example.com", "kata-sandi-1");
        session.Register(UserRole.RestaurantOwner, "Bima", "bima@example.com", "kata-sandi-2",
            new Replate.Domain.Services.RestaurantProfile("Bakmi Bima", "Jl. Magelang 5", "0812 3456 789", new TimeOnly(17, 0), new TimeOnly(1, 0)));
        session.AsOwner("Warung Sambal Ijo");
        var sambalMenu = session.SaveMenu(null, "Es teh sereh", "Teh dingin dengan sereh.", MenuCategories.LegacyProduce, 8000, .2m);
        Ensure(sambalMenu.Category == MenuCategories.Produce, "Saving the old Vegetarian category stores Buah & sayur");
        var savedText = File.ReadAllText(seededPath);
        Ensure(Version(seededPath) == 2, "Save writes version 2");
        var roundTrip = new DemoSession(seededPath);
        Ensure(roundTrip.StorageError is null, "Version 2 file loads: " + roundTrip.StorageError);
        Ensure(roundTrip.Restaurants.Select(r => (r.Id, r.Name, r.Address, r.OwnerId, r.Owner?.Id, r.Description, r.Phone, r.OpeningTime, r.ClosingTime))
            .SequenceEqual(session.Restaurants.Select(r => (r.Id, r.Name, r.Address, r.OwnerId, r.Owner?.Id, r.Description, r.Phone, r.OpeningTime, r.ClosingTime))),
            "Restaurants round trip");
        Ensure(roundTrip.Restaurants.SelectMany(r => r.Menus).All(m => m.Restaurant!.Id == m.RestaurantId) &&
            roundTrip.FindRestaurant(sambal.Id)!.Menus.Any(m => m.Id == sambalMenu.Id), "Menus round trip into their restaurants");
        Ensure(roundTrip.Users.Select(u => (u.Id, u.Name, u.Email, u.PasswordHash, u.Role))
            .SequenceEqual(session.Users.Select(u => (u.Id, u.Name, u.Email, u.PasswordHash, u.Role))), "Users round trip");
        Ensure(roundTrip.AllOrders.Count() == session.AllOrders.Count() && roundTrip.Listings.Count == session.Listings.Count &&
            roundTrip.Carts.Select(c => (c.Id, c.CustomerId)).SequenceEqual(session.Carts.Select(c => (c.Id, c.CustomerId))),
            "Orders, listings, and carts round trip");
        var copyPath = Path.Combine(directory, "copy.json");
        SessionStore.Save(roundTrip, copyPath);
        Ensure(File.ReadAllText(copyPath) == savedText, "Version 2 save and load are lossless");
        Ensure(!File.Exists(seededPath + ".v1.bak"), "Version 2 files are not backed up as version 1");

        Directory.Delete(directory, true);
        Console.WriteLine("All Replate multi-restaurant checks passed.");
    }

    private sealed record Fixture(object Json, Guid RestaurantId, Guid MenuId, Guid ListingId, Guid OrderId, Guid CartId, Guid CustomerId);

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
        return new Fixture(json, restaurantId, menuId, listingId, orderId, cartId, customerId);
    }

    private static int Version(string path) => JsonNode.Parse(File.ReadAllText(path))!["Version"]!.GetValue<int>();

    private static void Ensure(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }

    private static void ExpectFailure(Action action) => FailureMessage(action);

    private static string FailureMessage(Action action)
    {
        try { action(); } catch (InvalidOperationException e) { return e.Message; }
        throw new InvalidOperationException("Expected validation failure.");
    }

    private sealed class TestOrderStore(bool fail) : Data.IOrderStore
    {
        internal const string Message = "Pesanan dibuat, tetapi belum dapat disimpan ke PostgreSQL. Periksa koneksi database lalu coba lagi.";
        public List<Order> Calls { get; } = [];

        public Task SaveOrderAsync(Order order)
        {
            Calls.Add(order);
            return fail ? Task.FromException(new InvalidOperationException(Message)) : Task.CompletedTask;
        }
    }
}
