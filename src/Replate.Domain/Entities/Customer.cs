using Replate.Domain.Enums;
using Replate.Domain.Services;
using Replate.Domain.ValueObjects;

namespace Replate.Domain.Entities;

public sealed class Customer : User
{
    public override UserRole Role => UserRole.Customer;
    public Cart Cart { get; set; } = new();
    public List<Order> Orders { get; } = [];
    public GeoPoint? Location { get; private set; }
    public string? LocationLabel { get; private set; }

    public void SetLocation(GeoPoint location, string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new ArgumentException("Label lokasi wajib diisi.", nameof(label));
        Location = location;
        LocationLabel = label.Trim();
    }

    public void ClearLocation()
    {
        Location = null;
        LocationLabel = null;
    }

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
