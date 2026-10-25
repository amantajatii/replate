using Replate.Domain.Enums;

namespace Replate.Desktop;

internal static class StateVerification
{
    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "replate-check-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "state.json");
        var session = new DemoSession(path);
        var listingId = session.Listings[0].Id;
        session.AddToCart(session.Listings[0], 2);
        session = new DemoSession(path);
        Ensure(session.StorageError is null && session.Cart.Items.Single().Quantity == 2, "Cart survives restart");
        var listing = session.Listings.Single(l => l.Id == listingId);
        var order = session.Checkout();
        Ensure(order.TotalPrice == 32000 && listing.Quantity == 4, "Checkout snapshots and reserves stock");
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
        session = new DemoSession(path);
        order = session.CustomerOrders.Single();
        Ensure(order.Status == OrderStatus.Completed && order.CompletedAt == completedAt, "Status and timestamp survive restart");
        Ensure(order.Items.Single().Listing.Menu.Name == "Chicken rice bowl", "Navigation references restored");
        listing = session.Listings.Single(l => l.Id == listingId);
        session.AddToCart(listing, 1);
        var cancelled = session.Checkout(); session.CancelOrder(cancelled);
        ExpectFailure(() => session.CancelOrder(cancelled));
        Ensure(listing.Quantity == 4, "Cancellation restores stock exactly once");
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
        session.AddToCart(listing, 1);
        listing.PickupEnd = DateTime.UtcNow.AddSeconds(-1); session.Persist();
        session = new DemoSession(path);
        Ensure(session.Cart.Items.Count == 1 && !session.Cart.ValidateCart(), "Expired cart retained but cannot checkout");
        Ensure(!session.Search("", "Semua").Any(l => l.Id == listingId), "Expired offer hidden after restart");
        session.ChangeQuantity(listingId, 0);
        Ensure(session.Cart.Items.Count == 0, "Expired cart can be cleared");
        var corrupt = Path.Combine(directory, "corrupt.json"); File.WriteAllText(corrupt, "invalid json");
        var broken = new DemoSession(corrupt);
        ExpectFailure(broken.Persist);
        Ensure(broken.StorageError is not null && File.ReadAllText(corrupt) == "invalid json", "Corrupt storage is preserved read-only");
        var removable = session.SaveMenu(null, "Menu tanpa relasi", "", "Makanan utama", 20000, .3m);
        session.DeleteMenu(removable);
        session = new DemoSession(path);
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
        session.Cart.Items.Add(cartItem);
        ExpectFailure(() => session.DeleteMenu(probe));
        session.Cart.Items.Remove(cartItem);
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
        var allocationSession = new DemoSession();
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
        var allocationOrder = allocationSession.Checkout(); CheckMinimum();
        allocationSession.ConfirmOrder(allocationOrder, 15); allocationSession.ReadyOrder(allocationOrder);
        allocationSession.CompletePickup(allocationOrder, allocationOrder.PickupCode); CheckMinimum();
        allocationSession.AddToCart(allocationListing, 1);
        allocationOrder = allocationSession.Checkout(); CheckMinimum();
        allocationSession.CancelOrder(allocationOrder); CheckMinimum();
        var allErrors = allocationSession.ValidateProduction(null, DateOnly.FromDateTime(DateTime.Today.AddDays(1)), -1, -2);
        Ensure(allErrors.Count == 4, "Production validation reports all fields together");
        Console.WriteLine("All Replate desktop state checks passed.");
    }
    private static void Ensure(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
    private static void ExpectFailure(Action action)
    {
        try { action(); } catch (InvalidOperationException) { return; }
        throw new InvalidOperationException("Expected validation failure.");
    }
}
