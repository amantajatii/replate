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

    public void AddToCart(SurplusListing listing, int quantity) => GetOwnedCart().AddItem(listing, quantity);

    public Order Checkout(CheckoutService checkout, Guid restaurantId)
    {
        var order = checkout.CreateOrder(GetOwnedCart(), restaurantId);
        Orders.Add(order);
        return order;
    }

    public IReadOnlyList<Order> ViewOrders()
    {
        RequireAuthentication();
        return Orders.Where(order => order.CustomerId == Id).ToList();
    }

    private Cart GetOwnedCart()
    {
        RequireAuthentication();
        if (Cart.CustomerId != Guid.Empty && Cart.CustomerId != Id)
        {
            throw new UnauthorizedAccessException("Keranjang ini milik pelanggan lain.");
        }

        Cart.CustomerId = Id;
        return Cart;
    }
}
