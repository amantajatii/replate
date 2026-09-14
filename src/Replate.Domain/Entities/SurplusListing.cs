using Replate.Domain.Enums;

namespace Replate.Domain.Entities;

public sealed class SurplusListing
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid MenuId { get; set; }
    public Menu Menu { get; set; } = new();
    public decimal RescuePrice { get; set; }
    public int Quantity { get; set; }
    public DateTime PickupStart { get; set; }
    public DateTime PickupEnd { get; set; }
    public ListingStatus Status { get; private set; } = ListingStatus.Active;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public void UpdateStock(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(quantity);
        Quantity = quantity;
        if (quantity == 0 && Status == ListingStatus.Active)
        {
            MarkSoldOut();
        }
        else if (quantity > 0 && Status == ListingStatus.SoldOut)
        {
            Status = ListingStatus.Active;
        }
    }

    public void MarkSoldOut() => Status = ListingStatus.SoldOut;

    public void MarkExpired() => Status = ListingStatus.Expired;

    public void Cancel() => Status = ListingStatus.Cancelled;

    public decimal CalculateDiscount()
    {
        if (Menu.NormalPrice <= 0)
        {
            return 0;
        }

        return (Menu.NormalPrice - RescuePrice) / Menu.NormalPrice * 100;
    }

    public bool IsAvailable(DateTime now) =>
        Status == ListingStatus.Active && Quantity > 0 && now >= PickupStart && now <= PickupEnd;
}
