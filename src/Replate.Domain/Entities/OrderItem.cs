namespace Replate.Domain.Entities;

public sealed class OrderItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid ListingId { get; set; }
    public SurplusListing Listing { get; set; } = new();
    public string MenuName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public decimal CalculateSubtotal() => Quantity * UnitPrice;
}
