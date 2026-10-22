namespace Replate.Domain.Entities;

public sealed class CartItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CartId { get; set; }
    public Guid ListingId { get; set; }
    public SurplusListing Listing { get; set; } = new();
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public decimal CalculateSubtotal() => Quantity * UnitPrice;
}
