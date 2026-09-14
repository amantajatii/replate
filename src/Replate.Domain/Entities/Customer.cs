using Replate.Domain.Enums;
using Replate.Domain.Services;

namespace Replate.Domain.Entities;

public sealed class Customer : User
{
    public override UserRole Role => UserRole.Customer;
    public Cart Cart { get; set; } = new();
    public List<Order> Orders { get; } = [];

    public IEnumerable<SurplusListing> BrowseMarketplace(MarketplaceService marketplace) =>
        marketplace.GetActiveListings();

    public IEnumerable<SurplusListing> SearchListing(MarketplaceService marketplace, string keyword) =>
        marketplace.SearchByFood(keyword);

    public void AddToCart(SurplusListing listing, int quantity) => Cart.AddItem(listing, quantity);

    public Order Checkout(CheckoutService checkout)
    {
        var order = checkout.CreateOrder(Cart);
        Orders.Add(order);
        return order;
    }

    public IReadOnlyList<Order> ViewOrders() => Orders;
}
