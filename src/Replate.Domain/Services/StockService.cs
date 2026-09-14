using Replate.Domain.Entities;
using Replate.Domain.Enums;

namespace Replate.Domain.Services;

public sealed class StockService
{
    public void ReserveStock(SurplusListing listing, int quantity) => ReduceStock(listing, quantity);

    public void ReduceStock(SurplusListing listing, int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        if (listing.Status != ListingStatus.Active || listing.Quantity < quantity)
        {
            throw new InvalidOperationException("Insufficient listing stock.");
        }

        listing.UpdateStock(listing.Quantity - quantity);
    }

    public void RestoreStock(SurplusListing listing, int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        listing.UpdateStock(checked(listing.Quantity + quantity));
    }

    public void UpdateListingStatus(SurplusListing listing, DateTime? now = null)
    {
        if (listing.Quantity == 0)
        {
            listing.MarkSoldOut();
        }
        else if ((now ?? DateTime.UtcNow) > listing.PickupEnd)
        {
            listing.MarkExpired();
        }
    }
}
