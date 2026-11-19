using Replate.Domain.Entities;
using Replate.Domain.Enums;
using Replate.Domain.Services;

var customer = new Customer { Name = "Budi", Email = "budi@example.com" };

Ensure(customer is User, "Customer must inherit User");
Ensure(customer.Role == UserRole.Customer, "Customer role must be Customer");

var record = new ProductionRecord
{
    ProducedQuantity = 20,
    RegularSoldQuantity = 12,
    RescuedQuantity = 3
};

Ensure(record.CalculateSurplus() == 8, "Surplus must be produced minus regular sold");
Ensure(record.CalculateWasted() == 5, "Waste must be surplus minus rescued");

var listing = new SurplusListing
{
    Menu = new Menu { Name = "Nasi Goreng", NormalPrice = 20_000m },
    RescuePrice = 10_000m,
    Quantity = 3,
    PickupStart = DateTime.UtcNow.AddMinutes(-5),
    PickupEnd = DateTime.UtcNow.AddHours(1)
};

Ensure(listing.CalculateDiscount() == 50m, "Discount must be 50 percent");
Ensure(listing.IsAvailable(DateTime.UtcNow), "Listing must be available in pickup window");

var cart = new Cart();
cart.AddItem(listing, 2);
Ensure(cart.CalculateSubtotal() == 20_000m, "Cart subtotal must use rescue price");

var stock = new StockService();
var checkout = new CheckoutService(stock);
var order = checkout.CreateOrder(cart);

Ensure(order.TotalPrice == 20_000m, "Order total must equal cart subtotal");
Ensure(listing.Quantity == 1, "Checkout must reserve listing stock");

order.Confirm();
order.SetPreparationTime(TimeSpan.FromMinutes(15));
order.MarkReady();

var verification = new PickupVerification { Order = order, EnteredCode = order.PickupCode };
Ensure(verification.VerifyCode(), "Matching pickup code must verify");
verification.CompletePickup();
Ensure(order.Status == OrderStatus.Completed, "Pickup must complete the order");

var dashboard = new SurplusDashboard();
Ensure(dashboard.CalculateTotalProduced([record]) == 20, "Dashboard must total production");

// Multi-restaurant cart
var firstRestaurant = new Restaurant { Name = "Dapur Bumi" };
var secondRestaurant = new Restaurant { Name = "Warung Sambal Ijo" };
SurplusListing ListingAt(Restaurant restaurant, string name) => new()
{
    Menu = new Menu { Name = name, NormalPrice = 20_000m, RestaurantId = restaurant.Id, Restaurant = restaurant },
    RescuePrice = 10_000m,
    Quantity = 3,
    PickupStart = DateTime.UtcNow.AddMinutes(-5),
    PickupEnd = DateTime.UtcNow.AddHours(1)
};
var firstListing = ListingAt(firstRestaurant, "Chicken rice bowl");
var secondListing = ListingAt(secondRestaurant, "Gado-gado");
var multiCart = new Cart();
multiCart.AddItem(firstListing, 1);
Ensure(multiCart.RestaurantId == firstRestaurant.Id, "Cart must take the restaurant of its first item");
var rejected = false;
try { multiCart.AddItem(secondListing, 1); }
catch (InvalidOperationException) { rejected = true; }
Ensure(rejected && multiCart.Items.Count == 1 && secondListing.Quantity == 3, "Cart must reject items from another restaurant");
Ensure(checkout.CreateOrder(multiCart).RestaurantId == firstRestaurant.Id, "Order must belong to the cart restaurant");
multiCart.AddItem(secondListing, 1);
Ensure(multiCart.RestaurantId == secondRestaurant.Id, "Cleared cart may switch restaurant");
var marketplace = new MarketplaceService([firstListing, secondListing]);
Ensure(marketplace.SearchByRestaurant("sambal").Single() == secondListing, "Marketplace must search by restaurant name");

// Password hashing
var storedHash = PasswordHasher.Hash("rahasia-123");
var hashParts = storedHash.Split('$');
Ensure(hashParts.Length == 4 && hashParts[0] == "pbkdf2-sha256" && hashParts[1] == "210000" &&
    Convert.FromBase64String(hashParts[2]).Length == 16 && Convert.FromBase64String(hashParts[3]).Length == 32, "Hash must use the stored format");
Ensure(!storedHash.Contains("rahasia-123"), "Hash must not contain the password");
Ensure(PasswordHasher.Verify("rahasia-123", storedHash), "Hash must verify the same password");
Ensure(!PasswordHasher.Verify("rahasia-124", storedHash) && !PasswordHasher.Verify("", storedHash), "Hash must reject a wrong password");
var secondHash = PasswordHasher.Hash("rahasia-123");
Ensure(secondHash != storedHash && secondHash.Split('$')[2] != hashParts[2] && PasswordHasher.Verify("rahasia-123", secondHash),
    "Same password must use a different salt");
var lowIterations = $"pbkdf2-sha256$1000${hashParts[2]}$" + Convert.ToBase64String(
    System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2("rahasia-123", Convert.FromBase64String(hashParts[2]), 1000,
        System.Security.Cryptography.HashAlgorithmName.SHA256, 32));
Ensure(PasswordHasher.Verify("rahasia-123", lowIterations), "Verify must read iterations from the stored hash");
foreach (var broken in new[] { "", "plain", "pbkdf2-sha256$210000$abc", "md5$210000$" + hashParts[2] + "$" + hashParts[3],
    "pbkdf2-sha256$x$" + hashParts[2] + "$" + hashParts[3], "pbkdf2-sha256$0$" + hashParts[2] + "$" + hashParts[3],
    "pbkdf2-sha256$-5$" + hashParts[2] + "$" + hashParts[3], "pbkdf2-sha256$210000$!!!$" + hashParts[3],
    "pbkdf2-sha256$210000$" + hashParts[2] + "$", storedHash + "$extra" })
    Ensure(!PasswordHasher.Verify("rahasia-123", broken), "Broken hash must be rejected: " + broken);
Ensure(!PasswordHasher.Verify(null, storedHash) && !PasswordHasher.Verify("rahasia-123", null), "Null inputs must be rejected");

// Account validation
var accounts = new List<User> { AccountService.CreateCustomer([], "  Sari  ", "  Sari@Example.COM ", "kata-sandi-1") };
Ensure(accounts[0].Name == "Sari" && accounts[0].Email == "sari@example.com" && accounts[0].PasswordHash.StartsWith("pbkdf2-sha256$", StringComparison.Ordinal),
    "Registration must trim name and normalize email");
var accountErrors = AccountService.ValidateAccount(accounts, "", "SARI@example.com", "pendek");
Ensure(accountErrors["name"] == "Nama wajib diisi." && accountErrors["email"] == "Email sudah terdaftar." &&
    accountErrors["password"] == "Kata sandi minimal 8 karakter.", "Registration must report all account errors");
Ensure(AccountService.ValidateAccount(accounts, new string('a', 81), "bukan-email", "12345678")["email"] == "Format email tidak valid." &&
    AccountService.ValidateAccount(accounts, new string('a', 81), "x@y.id", "12345678")["name"] == "Nama maksimal 80 karakter.",
    "Registration must validate email format and name length");
Ensure(AccountService.ValidateAccount(accounts, "Budi", "budi@example.com", "12345678").Count == 0, "Valid registration has no errors");
Ensure(accountErrors.Values.All(message => !message.Contains("pendek")), "Errors must not contain the password");
var profile = new RestaurantProfile("Dapur Malam", "Jl. Malioboro 1", "+62 812-3456-7890", new TimeOnly(18, 0), new TimeOnly(2, 0), "Buka sampai dini hari.");
Ensure(AccountService.ValidateRestaurantProfile(profile).Count == 0, "Overnight opening hours must be valid");
var profileErrors = AccountService.ValidateRestaurantProfile(new RestaurantProfile(" ", "", "0812-ABC", null, null, new string('x', 501)));
Ensure(profileErrors.Keys.Order().SequenceEqual(new[] { "address", "closingTime", "description", "openingTime", "phone", "restaurantName" }),
    "Restaurant profile must report all errors");
Ensure(AccountService.ValidateRestaurantProfile(profile with { Phone = "1234567" })["phone"].Contains("8–15 digit") &&
    AccountService.ValidateRestaurantProfile(profile with { Phone = "1234567890123456" })["phone"].Contains("8–15 digit") &&
    AccountService.ValidateRestaurantProfile(profile with { Name = new string('r', 81) })["restaurantName"] == "Nama restoran maksimal 80 karakter.",
    "Restaurant phone digits and name length must be validated");
var owner = AccountService.CreateOwner(accounts, "Rina", "Rina@Example.com", "kata-sandi-2", profile);
var ownedRestaurant = owner.Restaurants.Single();
Ensure(ownedRestaurant.OwnerId == owner.Id && ownedRestaurant.Owner == owner && ownedRestaurant.Name == "Dapur Malam" &&
    ownedRestaurant.Phone == "+62 812-3456-7890" && ownedRestaurant.IsOpen(new TimeOnly(1, 0)) && !ownedRestaurant.IsOpen(new TimeOnly(12, 0)),
    "Owner registration must create an owned restaurant with its profile");
accounts.Add(owner);
var ownerRejected = false;
try { AccountService.CreateOwner(accounts, "Rina", "rina@example.com", "kata-sandi-2", profile); }
catch (InvalidOperationException) { ownerRejected = true; }
Ensure(ownerRejected, "Duplicate owner email must be rejected");

// Authentication
Ensure(AccountService.Authenticate(accounts, " RINA@example.com ", "kata-sandi-2") == owner, "Sign-in must ignore email case");
Ensure(AccountService.Authenticate(accounts, "rina@example.com", "salah-sandi") is null &&
    AccountService.Authenticate(accounts, "tidak-ada@example.com", "kata-sandi-2") is null &&
    AccountService.Authenticate(accounts, "", "") is null, "Failed sign-in must return no user");
Ensure(AccountService.SignInFailedMessage == "Email atau kata sandi salah.", "Failed sign-in must use the generic message");

Console.WriteLine("All Replate domain checks passed.");

static void Ensure(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
