namespace Replate.Domain.Entities;

public sealed class Cart
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    [System.Text.Json.Serialization.JsonInclude]
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
    public List<CartItem> Items { get; } = [];

    public void AddItem(SurplusListing listing, int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        if (!listing.IsAvailable(DateTime.UtcNow) || quantity > listing.Quantity)
        {
            throw new InvalidOperationException("Listing stock is unavailable.");
        }

        var item = Items.SingleOrDefault(candidate => candidate.ListingId == listing.Id);
        if (item is null)
        {
            Items.Add(new CartItem
            {
                CartId = Id,
                ListingId = listing.Id,
                Listing = listing,
                Quantity = quantity,
                UnitPrice = listing.RescuePrice
            });
        }
        else
        {
            if (item.Quantity + quantity > listing.Quantity)
            {
                throw new InvalidOperationException("Requested quantity exceeds listing stock.");
            }

            item.Quantity += quantity;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveItem(Guid listingId)
    {
        Items.RemoveAll(item => item.ListingId == listingId);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateQuantity(Guid listingId, int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        var item = Items.Single(item => item.ListingId == listingId);
        if (quantity > item.Listing.Quantity)
        {
            throw new InvalidOperationException("Requested quantity exceeds listing stock.");
        }

        item.Quantity = quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearCart()
    {
        Items.Clear();
        UpdatedAt = DateTime.UtcNow;
    }

    public decimal CalculateSubtotal() => Items.Sum(item => item.CalculateSubtotal());

    public IReadOnlyList<CartRestaurantGroup> GroupByRestaurant() =>
        Items.GroupBy(item => item.Listing.Menu.RestaurantId)
            .Select(group => new CartRestaurantGroup(group.Key, [.. group]))
            .ToList();

    public CartRestaurantGroup? FindGroup(Guid restaurantId) =>
        GroupByRestaurant().FirstOrDefault(group => group.RestaurantId == restaurantId);

    public int CountRestaurants() => Items.Select(item => item.Listing.Menu.RestaurantId).Distinct().Count();
}
