using System.Text.Json.Nodes;
using Replate.Domain.Entities;
using Replate.Domain.Enums;
using Replate.Domain.Services;

namespace Replate.Desktop;

internal static class AccountVerification
{
    public static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), "replate-accounts-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "state.json");

        // Demo accounts
        var session = new DemoSession(path);
        Ensure(session.Users.Count == 4 && session.Users.OfType<RestaurantOwner>().Count() == 3 &&
            session.Restaurants.All(r => r.Owner is not null && r.OwnerId == r.Owner.Id && r.Owner.Restaurants.Single() == r),
            "Seeded restaurants receive demo owners");
        Ensure(session.Users.Select(u => u.Email).Order().SequenceEqual(new[] { "dapur-bumi@replate.demo", "pelanggan@replate.demo",
            "roti-pagi-kotabaru@replate.demo", "warung-sambal-ijo@replate.demo" }), "Demo account emails use restaurant slugs");
        Ensure(session.Users.All(u => PasswordHasher.Verify(DemoAccounts.Password, u.PasswordHash)), "Demo accounts use the demo password");
        Ensure(!session.IsSignedIn && session.CurrentUser is null && !session.CustomerOrders.Any(), "Session starts signed out");
        session.Persist();
        var demoUsers = session.Users.Select(u => (u.Id, u.Email, u.PasswordHash)).ToList();
        session = new DemoSession(path);
        Ensure(session.Users.Select(u => (u.Id, u.Email, u.PasswordHash)).SequenceEqual(demoUsers), "Demo accounts are idempotent after reload");

        // Sign in
        var bumi = session.Restaurants.Single(r => r.Name == "Dapur Bumi");
        var sambal = session.Restaurants.Single(r => r.Name == "Warung Sambal Ijo");
        session.SignIn("  DAPUR-BUMI@replate.demo ", DemoAccounts.Password);
        Ensure(session.IsSignedIn && session.CurrentUser is RestaurantOwner && session.Restaurant == bumi, "Owner sign-in selects the owned restaurant");
        session.SignIn("warung-sambal-ijo@replate.demo", DemoAccounts.Password);
        Ensure(session.Restaurant == sambal, "Another owner sign-in selects that owner's restaurant");
        var signedIn = session.CurrentUser;
        var wrongPassword = FailureMessage(() => session.SignIn(DemoAccounts.CustomerEmail, "replate124"));
        var unknownEmail = FailureMessage(() => session.SignIn("tidak-ada@replate.demo", DemoAccounts.Password));
        var emptyInput = FailureMessage(() => session.SignIn("", ""));
        Ensure(wrongPassword == "Email atau kata sandi salah." && unknownEmail == wrongPassword && emptyInput == wrongPassword,
            "Failed sign-in uses one generic message");
        Ensure(session.CurrentUser == signedIn, "Failed sign-in keeps the current account");
        session.SignOut();
        Ensure(session.CurrentUser is null && !session.IsSignedIn && session.Cart.Items.Count == 0 && session.LastOrder is null, "Sign-out clears the account");

        // Registration validation
        var usersBefore = session.Users.Count; var restaurantsBefore = session.Restaurants.Count;
        var profile = new RestaurantProfile("Bakmi Malam", "Jl. Magelang No. 5, Yogyakarta", "+62 812-3456-7890",
            new TimeOnly(17, 0), new TimeOnly(1, 0), "Bakmi godog dan goreng sampai dini hari.");
        Ensure(FailureMessage(() => session.Register(UserRole.Customer, "Ganda", "Pelanggan@REPLATE.demo", "kata-sandi-1")) == "Email sudah terdaftar.",
            "Duplicate email with different case is rejected");
        Ensure(FailureMessage(() => session.Register(UserRole.Customer, "Ganda", "ganda@example.com", "pendek")) == "Kata sandi minimal 8 karakter.",
            "Short password is rejected");
        var ownerErrors = session.ValidateRegistration(UserRole.RestaurantOwner, "Rina", "rina@example.com", "kata-sandi-2",
            profile with { Name = "", Phone = "12-AB", OpeningTime = null });
        Ensure(ownerErrors.Keys.Order().SequenceEqual(new[] { "openingTime", "phone", "restaurantName" }), "Invalid restaurant profile is reported");
        ExpectFailure(() => session.Register(UserRole.RestaurantOwner, "Rina", "rina@example.com", "kata-sandi-2", profile with { Address = " " }));
        ExpectFailure(() => session.Register(UserRole.RestaurantOwner, "Rina", "rina@example.com", "kata-sandi-2"));
        var leaked = FailureMessage(() => session.Register(UserRole.Customer, "", "x", "rahasia"));
        Ensure(!leaked.Contains("rahasia"), "Validation messages do not reveal the password");
        Ensure(session.Users.Count == usersBefore && session.Restaurants.Count == restaurantsBefore, "Rejected registration changes nothing");

        // Owner registration
        var rina = (RestaurantOwner)session.Register(UserRole.RestaurantOwner, " Rina ", " Rina@Example.com ", "kata-sandi-2", profile);
        var rinaRestaurant = rina.Restaurants.Single();
        Ensure(rina.Email == "rina@example.com" && rinaRestaurant.OwnerId == rina.Id && rinaRestaurant.Owner == rina &&
            session.Restaurants.Contains(rinaRestaurant) && session.CurrentUser is null, "Owner registration creates an owned restaurant");
        var sari = (Customer)session.Register(UserRole.Customer, "Sari", "sari@example.com", "kata-sandi-1");
        var stored = File.ReadAllText(path);
        var storedHashes = JsonNode.Parse(stored)!["Users"]!.AsArray().Select(u => u!["PasswordHash"]!.GetValue<string>()).ToList();
        Ensure(!stored.Contains("kata-sandi-1") && !stored.Contains("kata-sandi-2") && !stored.Contains(DemoAccounts.Password) &&
            !stored.Contains("\"Password\"") && storedHashes.Contains(rina.PasswordHash) && storedHashes.Contains(sari.PasswordHash) &&
            storedHashes.All(h => h.StartsWith("pbkdf2-sha256$210000$", StringComparison.Ordinal)), "Only password hashes are stored");
        session = new DemoSession(path);
        var reloadedRina = session.Users.OfType<RestaurantOwner>().Single(u => u.Id == rina.Id);
        var reloadedRestaurant = session.FindRestaurant(rinaRestaurant.Id)!;
        Ensure(reloadedRestaurant.Owner == reloadedRina && reloadedRestaurant.OwnerId == rina.Id && reloadedRina.Restaurants.Single() == reloadedRestaurant &&
            reloadedRestaurant.Name == "Bakmi Malam" && reloadedRestaurant.Address == profile.Address && reloadedRestaurant.Phone == profile.Phone &&
            reloadedRestaurant.Description == profile.Description && reloadedRestaurant.OpeningTime == profile.OpeningTime &&
            reloadedRestaurant.ClosingTime == profile.ClosingTime, "Owner and restaurant profile round trip");
        Ensure(session.Users.Count == usersBefore + 2, "Registered accounts are not duplicated by demo setup");
        session.SignIn("rina@example.com", "kata-sandi-2");
        Ensure(session.Restaurant == reloadedRestaurant, "Registered owner signs in to the new restaurant");

        // Role checks
        bumi = session.Restaurants.Single(r => r.Name == "Dapur Bumi");
        var bumiListing = session.Listings.First(l => l.Menu.RestaurantId == bumi.Id && l.IsAvailable(DateTime.UtcNow));
        var bumiMenu = bumiListing.Menu;
        session.SignOut();
        ExpectFailure(() => session.AddToCart(bumiListing, 1));
        ExpectFailure(() => session.SaveMenu(null, "Menu tamu", "", "Makanan utama", 20000, .3m));
        ExpectFailure(() => session.ToggleMenu(bumiMenu));
        session.AsOwner();
        ExpectFailure(() => session.AddToCart(bumiListing, 1));
        ExpectFailure(() => session.Checkout());
        ExpectFailure(() => session.ChangeQuantity(bumiListing.Id, 0));
        session.AsCustomer();
        ExpectFailure(() => session.SaveMenu(null, "Menu pelanggan", "", "Makanan utama", 20000, .3m));
        ExpectFailure(() => session.ToggleMenu(bumiMenu));
        ExpectFailure(() => session.SelectRestaurant(bumi.Id));
        Ensure(bumiMenu.IsActive && bumi.Menus.All(m => m.Name != "Menu pelanggan"), "Wrong roles change nothing");

        // Customer carts
        var demoCustomer = (Customer)session.CurrentUser!;
        session.AddToCart(bumiListing, 1);
        var demoOrder = session.Checkout();
        session.AddToCart(bumiListing, 2);
        Ensure(demoOrder.CustomerId == demoCustomer.Id && session.CustomerOrders.Single() == demoOrder, "Checkout uses the signed-in customer");
        session.SignIn("sari@example.com", "kata-sandi-1");
        var sambalListing = session.Listings.First(l => l.Menu.RestaurantId == sambal.Id && l.IsAvailable(DateTime.UtcNow));
        Ensure(session.Cart.Items.Count == 0 && !session.CustomerOrders.Any(), "Each customer starts with an own cart and history");
        ExpectFailure(() => session.CancelOrder(demoOrder));
        Ensure(demoOrder.Status == OrderStatus.Reserved, "Customer cannot cancel another customer's order");
        session.AddToCart(sambalListing, 1);
        session = new DemoSession(path);
        var carts = session.Carts.Where(c => c.Items.Count > 0).ToList();
        Ensure(carts.Count == 2 && carts.Select(c => c.CustomerId).Distinct().Count() == 2, "Carts are stored per customer");
        session.AsCustomer();
        Ensure(session.Cart.CustomerId == demoCustomer.Id && session.Cart.Items.Single().ListingId == bumiListing.Id &&
            session.Cart.Items.Single().Quantity == 2 && session.CustomerOrders.Single().Id == demoOrder.Id, "Demo customer cart survives reload");
        session.SignIn("SARI@example.com", "kata-sandi-1");
        Ensure(session.Cart.CustomerId == sari.Id && session.Cart.Items.Single().ListingId == sambalListing.Id, "Second customer cart survives reload");

        // Owner isolation
        session.SignIn("rina@example.com", "kata-sandi-2");
        bumi = session.Restaurants.Single(r => r.Name == "Dapur Bumi");
        bumiMenu = bumi.Menus[0];
        var bumiOffer = session.Listings.First(l => l.Menu.RestaurantId == bumi.Id && l.Status == ListingStatus.Active);
        var bumiOrder = bumi.Orders.Single(o => o.Id == demoOrder.Id);
        var menuName = bumiMenu.Name; var menuCount = bumi.Menus.Count; var offerPrice = bumiOffer.RescuePrice;
        var date = DateOnly.FromDateTime(bumiOffer.PickupStart.ToLocalTime());
        var production = bumiMenu.ProductionRecords.Single(r => r.RecordDate == date).ProducedQuantity;
        ExpectFailure(() => session.SaveMenu(bumiMenu, "Diambil alih", bumiMenu.Description, bumiMenu.Category, bumiMenu.NormalPrice, bumiMenu.WeightPerPortion));
        ExpectFailure(() => session.ToggleMenu(bumiMenu));
        ExpectFailure(() => session.DeleteMenu(bumiMenu));
        ExpectFailure(() => session.SaveProduction(bumiMenu, date, production + 5, 0));
        ExpectFailure(() => session.SaveListing(bumiOffer, bumiOffer.Menu, offerPrice - 1, bumiOffer.Quantity,
            bumiOffer.PickupStart.ToLocalTime(), bumiOffer.PickupEnd.ToLocalTime()));
        ExpectFailure(() => session.CancelListing(bumiOffer));
        ExpectFailure(() => session.ConfirmOrder(bumiOrder, 15));
        ExpectFailure(() => session.ReadyOrder(bumiOrder));
        ExpectFailure(() => session.CancelOrder(bumiOrder));
        ExpectFailure(() => session.CompletePickup(bumiOrder, bumiOrder.PickupCode));
        ExpectFailure(() => session.SelectRestaurant(bumi.Id));
        Ensure(bumiMenu.Name == menuName && bumiMenu.IsActive && bumi.Menus.Count == menuCount && bumiOffer.RescuePrice == offerPrice &&
            bumiOffer.Status == ListingStatus.Active && bumiOrder.Status == OrderStatus.Reserved &&
            bumiMenu.ProductionRecords.Single(r => r.RecordDate == date).ProducedQuantity == production, "Owner cannot change another restaurant");
        var rinaMenu = session.SaveMenu(null, "Bakmi godog", "Bakmi kuah.", "Makanan utama", 25000, .35m);
        Ensure(rinaMenu.RestaurantId == session.Restaurant.Id && session.Restaurant.Name == "Bakmi Malam" && bumi.Menus.Count == menuCount,
            "Owner changes only the own restaurant");
        session.AsOwner();
        session.ConfirmOrder(bumiOrder, 10);
        Ensure(bumiOrder.Status == OrderStatus.Confirmed, "Owning restaurant can process its order");

        // Version 2 without profile fields
        var legacyPath = Path.Combine(directory, "legacy-v2.json");
        var legacy = JsonNode.Parse(File.ReadAllText(path))!;
        foreach (var restaurant in legacy["Restaurants"]!.AsArray())
            foreach (var field in new[] { "OwnerId", "Description", "Phone", "OpeningTime", "ClosingTime" }) restaurant!.AsObject().Remove(field);
        legacy["Users"] = new JsonArray();
        File.WriteAllText(legacyPath, legacy.ToJsonString());
        var legacySession = new DemoSession(legacyPath);
        Ensure(legacySession.StorageError is null, "Version 2 file without profile fields loads: " + legacySession.StorageError);
        Ensure(legacySession.Restaurants.All(r => r.Description == "" && r.Phone == "" && r.OpeningTime == default && r.Owner is not null),
            "Missing profile fields use defaults and receive demo owners");
        Ensure(legacySession.Users.OfType<RestaurantOwner>().Count() == legacySession.Restaurants.Count &&
            legacySession.Users.OfType<Customer>().Single().Email == DemoAccounts.CustomerEmail, "Legacy version 2 data receives demo accounts");
        legacySession.Persist();
        Ensure(new DemoSession(legacyPath).Users.Count == legacySession.Users.Count, "Legacy demo accounts are not duplicated");

        var danglingPath = Path.Combine(directory, "dangling.json");
        var dangling = JsonNode.Parse(File.ReadAllText(path))!;
        dangling["Restaurants"]![0]!["OwnerId"] = Guid.NewGuid();
        File.WriteAllText(danglingPath, dangling.ToJsonString());
        Ensure(new DemoSession(danglingPath).StorageError is not null, "Unknown restaurant owner is rejected");

        Directory.Delete(directory, true);
        Console.WriteLine("All Replate account checks passed.");
    }

    private static string FailureMessage(Action action)
    {
        try { action(); } catch (InvalidOperationException e) { return e.Message; }
        throw new InvalidOperationException("Expected validation failure.");
    }

    private static void Ensure(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }

    private static void ExpectFailure(Action action) => FailureMessage(action);
}
