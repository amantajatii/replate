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

Console.WriteLine("All Replate domain checks passed.");

static void Ensure(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
