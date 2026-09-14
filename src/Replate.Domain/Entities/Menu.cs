namespace Replate.Domain.Entities;

public sealed class Menu
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal NormalPrice { get; set; }
    public decimal WeightPerPortion { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public List<ProductionRecord> ProductionRecords { get; } = [];
    public List<SurplusListing> SurplusListings { get; } = [];

    public void UpdateMenu(string name, string description, string category, decimal normalPrice, decimal weightPerPortion)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(normalPrice);
        ArgumentOutOfRangeException.ThrowIfNegative(weightPerPortion);
        Name = name;
        Description = description;
        Category = category;
        NormalPrice = normalPrice;
        WeightPerPortion = weightPerPortion;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
