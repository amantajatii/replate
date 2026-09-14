namespace Replate.Domain.Entities;

public sealed class Cart
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public Guid RestaurantId { get; private set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
    public List<CartItem> Items { get; } = [];

    public void AddItem(SurplusListing listing, int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        if (!listing.IsAvailable(DateTime.UtcNow) || quantity > listing.Quantity)
        {
            throw new InvalidOperationException("Listing stock is unavailable.");
        }

        var listingRestaurantId = listing.Menu.RestaurantId;
        if (Items.Count > 0 && RestaurantId != listingRestaurantId)
        {
            throw new InvalidOperationException("A cart can only contain items from one restaurant.");
        }

        var item = Items.SingleOrDefault(candidate => candidate.ListingId == listing.Id);
        if (item is null)
        {
            RestaurantId = listingRestaurantId;
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
        if (Items.Count == 0)
        {
            RestaurantId = Guid.Empty;
        }

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
        RestaurantId = Guid.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    public decimal CalculateSubtotal() => Items.Sum(item => item.CalculateSubtotal());

    public bool ValidateCart(DateTime? now = null)
    {
        var currentTime = now ?? DateTime.UtcNow;
        return Items.Count > 0
            && Items.All(item => item.Quantity > 0
                && item.Quantity <= item.Listing.Quantity
                && item.Listing.IsAvailable(currentTime)
                && item.Listing.Menu.RestaurantId == RestaurantId);
    }
}
