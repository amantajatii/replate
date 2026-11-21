namespace Replate.Domain.Entities;

public sealed record CartRestaurantGroup(Guid RestaurantId, IReadOnlyList<CartItem> Items)
{
    public decimal CalculateSubtotal() => Items.Sum(item => item.CalculateSubtotal());

    public bool Validate(DateTime? now = null)
    {
        var currentTime = now ?? DateTime.UtcNow;
        return Items.Count > 0
            && Items.All(item => item.Quantity > 0
                && item.Quantity <= item.Listing.Quantity
                && item.Listing.IsAvailable(currentTime)
                && item.Listing.Menu.IsActive);
    }
}
