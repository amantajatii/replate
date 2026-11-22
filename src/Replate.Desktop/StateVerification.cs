using Replate.Domain.Enums;

namespace Replate.Desktop;

internal static class StateVerification
{
    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "replate-check-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "state.json");
        var session = new DemoSession(path).AsCustomer();
        var listingId = session.Listings[0].Id;
        session.AddToCart(session.Listings[0], 2);
        session = new DemoSession(path).AsCustomer();
        Ensure(session.StorageError is null && session.Cart.Items.Single().Quantity == 2, "Cart survives restart");
        var listing = session.Listings.Single(l => l.Id == listingId);
        var order = session.Checkout(listing.Menu.RestaurantId);
        Ensure(order.TotalPrice == 32000 && listing.Quantity == 4, "Checkout snapshots and reserves stock");
        session.AsOwner();
        ExpectFailure(() => session.ConfirmOrder(order, -1));
        Ensure(order.Status == OrderStatus.Reserved, "Invalid confirmation is atomic");
        session.ConfirmOrder(order, 15); session.ReadyOrder(order);
        var today = listing.Menu.ProductionRecords.Single(r => r.RecordDate == DateOnly.FromDateTime(listing.PickupStart.ToLocalTime()));
        var rescuedBefore = today.RescuedQuantity;
        ExpectFailure(() => session.CompletePickup(order, "WRONG"));
        Ensure(order.Status == OrderStatus.ReadyForPickup && today.RescuedQuantity == rescuedBefore, "Wrong code changes nothing");
        session.CompletePickup(order, " " + order.PickupCode.ToLowerInvariant() + " ");
        Ensure(today.RescuedQuantity == rescuedBefore + 2, "Pickup updates production once");
        var completedAt = order.CompletedAt;
        ExpectFailure(() => session.CompletePickup(order, order.PickupCode));
        ExpectFailure(() => session.CancelOrder(order));
        Ensure(today.RescuedQuantity == rescuedBefore + 2 && listing.Quantity == 4, "Completed orders cannot mutate twice");
        session = new DemoSession(path).AsCustomer();
        order = session.CustomerOrders.Single();
        Ensure(order.Status == OrderStatus.Completed && order.CompletedAt == completedAt, "Status and timestamp survive restart");
        Ensure(order.Items.Single().Listing.Menu.Name == "Chicken rice bowl", "Navigation references restored");
        listing = session.Listings.Single(l => l.Id == listingId);
        session.AddToCart(listing, 1);
        var cancelled = session.Checkout(listing.Menu.RestaurantId); session.CancelOrder(cancelled);
        ExpectFailure(() => session.CancelOrder(cancelled));
        Ensure(listing.Quantity == 4, "Cancellation restores stock exactly once");
        today = listing.Menu.ProductionRecords.Single(r => r.RecordDate == DateOnly.FromDateTime(listing.PickupStart.ToLocalTime()));
        session.AddToCart(listing, 1);
        var overdue = session.Checkout(listing.Menu.RestaurantId);
        session.AsOwner();
        session.ConfirmOrder(overdue, 15);
        session.ReadyOrder(overdue);
        session.ExpireOverdueOrders(listing.PickupEnd);
        Ensure(overdue.Status == OrderStatus.ReadyForPickup, "Pickup is allowed through the stated deadline");
        var afterDeadline = listing.PickupEnd.AddTicks(1);
        session.ExpireOverdueOrders(afterDeadline);
        session.ExpireOverdueOrders(afterDeadline);
        Ensure(overdue.Status == OrderStatus.Cancelled && listing.Quantity == 4,
            "Expiration cancels an active order and restores stock only once");
        Ensure(!listing.IsAvailable(afterDeadline), "Restored stock cannot be reserved after the pickup deadline");
        Ensure(today.RescuedQuantity == rescuedBefore + 2, "Expired orders do not change rescued quantities");
        session = new DemoSession(path).AsCustomer();
        Ensure(session.CustomerOrders.Single(o => o.Id == overdue.Id).Status == OrderStatus.Cancelled &&
            session.Listings.Single(l => l.Id == listingId).Quantity == 4, "Expiration survives restart");
        session.AsOwner();
        var menu = session.SaveMenu(null, "Menu pengujian", "Menu lokal", "Makanan utama", 20000, .25m);
        var date = DateOnly.FromDateTime(DateTime.Today);
        session.SaveProduction(menu, date, 10, 6);
        var start = DateTime.Today.AddMinutes(1);
        var end = DateTime.Today.AddDays(1).AddSeconds(-1);
        ExpectFailure(() => session.SaveListing(null, menu, 10000, 5, start, end));
        var offer = session.SaveListing(null, menu, 10000, 4, start, end);
        ExpectFailure(() => session.SaveListing(null, menu, 10000, 1, start, end));
        ExpectFailure(() => session.SaveProduction(menu, date, 5, 4));
        ExpectFailure(() => session.SaveProduction(menu, date.AddDays(1), 10, 0));
        ExpectFailure(() => session.SaveListing(offer, menu, 30000, 4, start, end));
        session.ToggleMenu(menu);
        session = new DemoSession(path);
        Ensure(!session.Restaurant.Menus.Single(m => m.Id == menu.Id).IsActive, "Menu status survives restart");
        Ensure(session.Listings.Single(l => l.Id == offer.Id).Quantity == 4, "Listing survives restart");
        listing = session.Listings.Single(l => l.Id == listingId);
        session.AsCustomer();
        session.AddToCart(listing, 1);
        listing.PickupEnd = DateTime.UtcNow.AddSeconds(-1); session.Persist();
        session = new DemoSession(path).AsCustomer();
        Ensure(session.Cart.Items.Count == 1 && !session.Cart.GroupByRestaurant().Single().Validate(), "Expired cart retained but cannot checkout");
        Ensure(!session.Search("", "Semua").Any(l => l.Id == listingId), "Expired offer hidden after restart");
        session.ChangeQuantity(listingId, 0);
        Ensure(session.Cart.Items.Count == 0, "Expired cart can be cleared");
        var corrupt = Path.Combine(directory, "corrupt.json"); File.WriteAllText(corrupt, "invalid json");
        var broken = new DemoSession(corrupt);
        ExpectFailure(broken.Persist);
        Ensure(broken.StorageError is not null && File.ReadAllText(corrupt) == "invalid json", "Corrupt storage is preserved read-only");
        session.AsOwner();
        var removable = session.SaveMenu(null, "Menu tanpa relasi", "", "Makanan utama", 20000, .3m);
        session.DeleteMenu(removable);
        session = new DemoSession(path).AsOwner();
        Ensure(session.StorageError is null && session.Restaurant.Menus.All(m => m.Id != removable.Id), "Deleted menu stays deleted after restart");
        var usedMenu = session.Restaurant.Menus[0];
        ExpectFailure(() => session.DeleteMenu(usedMenu));
        Ensure(session.Restaurant.Menus.Contains(usedMenu), "Used menu retained");
        var probe = session.SaveMenu(null, "Menu uji relasi", "", "Makanan utama", 20000, .3m);
        var linked = new Replate.Domain.Entities.SurplusListing { MenuId = probe.Id, Menu = probe };
        session.Listings.Add(linked);
        ExpectFailure(() => session.DeleteMenu(probe));
        session.Listings.Remove(linked);
        probe.SurplusListings.Add(linked);
        ExpectFailure(() => session.DeleteMenu(probe));
        probe.SurplusListings.Clear();
        var cartItem = new Replate.Domain.Entities.CartItem { Listing = linked, ListingId = linked.Id };
        session.Carts.Single().Items.Add(cartItem);
        ExpectFailure(() => session.DeleteMenu(probe));
        session.Carts.Single().Items.Remove(cartItem);
        var historical = new Replate.Domain.Entities.Order();
        historical.Items.Add(new Replate.Domain.Entities.OrderItem { Listing = linked, ListingId = linked.Id });
        session.Restaurant.Orders.Add(historical);
        ExpectFailure(() => session.DeleteMenu(probe));
        session.Restaurant.Orders.Remove(historical);
        probe.ProductionRecords.Add(new Replate.Domain.Entities.ProductionRecord { MenuId = probe.Id, Menu = probe });
        ExpectFailure(() => session.DeleteMenu(probe));
        probe.ProductionRecords.Clear();
        session.Persist();
        // A directory at the temporary file path forces a real storage write failure.
        Directory.CreateDirectory(path + ".tmp");
        try
        {
            try { session.DeleteMenu(probe); throw new InvalidOperationException("Expected storage failure"); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
            Ensure(session.Restaurant.Menus.Contains(probe), "Failed deletion restores menu in memory");
            Ensure(new DemoSession(path).Restaurant.Menus.Any(m => m.Id == probe.Id), "Failed deletion preserves stored menu");
        }
        finally { Directory.Delete(path + ".tmp"); }
        var allocationSession = new DemoSession().AsCustomer();
        var allocationListing = allocationSession.Listings[0];
        var allocationMenu = allocationListing.Menu;
        var allocationDate = DateOnly.FromDateTime(allocationListing.PickupStart.ToLocalTime());
        void CheckMinimum()
        {
            var errors = allocationSession.ValidateProduction(allocationMenu, allocationDate, 41, 36);
            Ensure(errors["produced"] == "Jumlah produksi minimal 42 porsi.", "Minimum does not double count stock, orders, or completed pickup");
            Ensure(allocationSession.ValidateProduction(allocationMenu, allocationDate, 42, 36).Count == 0, "Exact production minimum is valid");
        }
        CheckMinimum();
        allocationSession.AddToCart(allocationListing, 2);
        var allocationOrder = allocationSession.Checkout(allocationListing.Menu.RestaurantId); CheckMinimum();
        allocationSession.AsOwner();
        allocationSession.ConfirmOrder(allocationOrder, 15); allocationSession.ReadyOrder(allocationOrder);
        allocationSession.CompletePickup(allocationOrder, allocationOrder.PickupCode); CheckMinimum();
        allocationSession.AsCustomer();
        allocationSession.AddToCart(allocationListing, 1);
        allocationOrder = allocationSession.Checkout(allocationListing.Menu.RestaurantId); CheckMinimum();
        allocationSession.CancelOrder(allocationOrder); CheckMinimum();
        var allErrors = allocationSession.ValidateProduction(null, DateOnly.FromDateTime(DateTime.Today.AddDays(1)), -1, -2);
        Ensure(allErrors.Count == 4, "Production validation reports all fields together");
        CheckLegacyCategory();
        var remotePath = Path.Combine(directory, "remote-state.json");
        var remoteSession = new DemoSession(remotePath).AsCustomer();
        var remoteRestaurant = new Replate.Domain.Entities.Restaurant { Name = "Restoran dari Supabase", Address = "Alamat pengambilan" };
        remoteRestaurant.SetLocation(VerificationGeoFakes.GeocodedPoint);
        var remoteMenu = new Replate.Domain.Entities.Menu
        {
            Restaurant = remoteRestaurant, RestaurantId = remoteRestaurant.Id, Name = "Menu bersama",
            NormalPrice = 20000m, WeightPerPortion = .25m
        };
        var remoteListing = new Replate.Domain.Entities.SurplusListing
        {
            Menu = remoteMenu, MenuId = remoteMenu.Id, RescuePrice = 10000m, Quantity = 2,
            PickupStart = DateTime.UtcNow.AddMinutes(-1), PickupEnd = DateTime.UtcNow.AddHours(1)
        };
        remoteSession.ApplySharedListings([remoteListing]);
        remoteSession.ApplySharedListings([remoteListing]);
        remoteSession.AddToCart(remoteListing, 1);
        remoteSession = new DemoSession(remotePath).AsCustomer();
        Ensure(remoteSession.Cart.GroupByRestaurant().Single().RestaurantId == remoteRestaurant.Id &&
            remoteSession.RestaurantLine(remoteRestaurant.Id).Contains(remoteRestaurant.Address) &&
            remoteSession.FindRestaurant(remoteRestaurant.Id) is { Owner: null } imported &&
            imported.Location == VerificationGeoFakes.GeocodedPoint,
            "Imported restaurant, coordinates and cart survive restart without creating a local owner");
        remoteRestaurant.Address = "Alamat baru tanpa koordinat";
        remoteRestaurant.SetLocation(null);
        remoteSession.ApplySharedListings([remoteListing]);
        Ensure(remoteSession.FindRestaurant(remoteRestaurant.Id) is { Location: null } updated && updated.Address == remoteRestaurant.Address,
            "Shared listing with a new address drops the old restaurant coordinates");
        CheckReopenListing(Path.Combine(directory, "reopen-state.json"));
        Console.WriteLine("All Replate desktop state checks passed.");
    }

    // Reopen listing
    private static void CheckReopenListing(string path)
    {
        var session = new DemoSession(path).AsOwner();
        var now = DateTime.UtcNow;
        var listing = session.RestaurantListings.First(l => l.IsAvailable(now) && l.Menu.IsActive);
        var (id, quantity, price, start, end) = (listing.Id, listing.Quantity, listing.RescuePrice, listing.PickupStart, listing.PickupEnd);
        ExpectFailure(() => session.ReopenListing(listing));
        session.CancelListing(listing);
        Ensure(!listing.IsAvailable(now) && DemoSession.ReopenBlocker(listing, now) is null, "Closed valid listing can be reopened");
        session.ReopenListing(listing);
        Ensure(listing.Status == ListingStatus.Active && listing.IsAvailable(DateTime.UtcNow), "Reopened listing is available again");
        session = new DemoSession(path).AsCustomer();
        listing = session.Listings.Single(l => l.Id == id);
        Ensure(listing.Status == ListingStatus.Active && listing.Quantity == quantity && listing.RescuePrice == price &&
            listing.PickupStart == start && listing.PickupEnd == end && session.Search("", "Semua").Any(l => l.Id == id),
            "Reopened listing survives restart unchanged and shows in the marketplace");
        session.AsOwner();
        session.CancelListing(listing);
        listing.PickupEnd = DateTime.UtcNow.AddSeconds(-1);
        Ensure(DemoSession.ReopenBlocker(listing, DateTime.UtcNow) == "Jendela pengambilan sudah berakhir", "Ended pickup window blocks reopening");
        ExpectFailure(() => session.ReopenListing(listing));
        Ensure(listing.Status == ListingStatus.Cancelled, "Ended listing stays closed");
        listing.PickupEnd = end;
        listing.UpdateStock(0);
        Ensure(DemoSession.ReopenBlocker(listing, DateTime.UtcNow) == "Stok habis", "Empty stock blocks reopening");
        ExpectFailure(() => session.ReopenListing(listing));
        Ensure(listing.Status == ListingStatus.Cancelled, "Sold out listing stays closed");
    }

    // Legacy category
    internal static string WriteLegacyState()
    {
        var directory = Path.Combine(Path.GetTempPath(), "replate-legacy-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "state.json");
        new DemoSession(path).Persist();
        var text = File.ReadAllText(path);
        var legacy = System.Text.RegularExpressions.Regex.Replace(text, @"""Category"": ""Buah (?:&|\\u0026) sayur""", "\"Category\": \"Vegetarian\"");
        Ensure(legacy != text && legacy.Split("\"Category\": \"Vegetarian\"").Length - 1 == 3 && !legacy.Contains(@"Buah & sayur") && !legacy.Contains("Buah & sayur"),
            "Legacy state file stores Vegetarian for three menus");
        File.WriteAllText(path, legacy);
        return path;
    }

    private static void CheckLegacyCategory()
    {
        var seeded = new DemoSession();
        var produce = seeded.Restaurants.SelectMany(r => r.Menus).Where(m => m.Category == MenuCategories.Produce).Select(m => m.Name).ToList();
        Ensure(produce.Count == 3 && seeded.Restaurants.SelectMany(r => r.Menus).All(m => MenuCategories.Names.Contains(m.Category)),
            "Seed data uses Buah & sayur");
        var path = WriteLegacyState();
        var before = File.ReadAllBytes(path);
        var stamp = File.GetLastWriteTimeUtc(path);
        var session = new DemoSession(path);
        var menus = session.Restaurants.SelectMany(r => r.Menus).ToList();
        Ensure(session.StorageError is null && menus.Where(m => m.Category == MenuCategories.Produce).Select(m => m.Name).OrderBy(n => n).SequenceEqual(produce.OrderBy(n => n)) &&
            !menus.Any(m => m.Category == MenuCategories.LegacyProduce), "Legacy Vegetarian menus load as Buah & sayur");
        Ensure(File.ReadAllBytes(path).SequenceEqual(before) && File.GetLastWriteTimeUtc(path) == stamp, "Loading a legacy file does not rewrite it");
        Ensure(session.Search("", MenuCategories.Produce).Count() == 3, "Legacy menus are found under Buah & sayur");
    }
    private static void Ensure(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
    private static void ExpectFailure(Action action)
    {
        try { action(); } catch (InvalidOperationException) { return; }
        throw new InvalidOperationException("Expected validation failure.");
    }
}
