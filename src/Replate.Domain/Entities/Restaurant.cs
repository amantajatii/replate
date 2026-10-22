namespace Replate.Domain.Entities;

public sealed class Restaurant
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    public RestaurantOwner? Owner { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public TimeOnly OpeningTime { get; set; }
    public TimeOnly ClosingTime { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<Menu> Menus { get; } = [];
    public List<Order> Orders { get; } = [];

    public void UpdateProfile(
        string name,
        string description,
        string address,
        string phone,
        TimeOnly openingTime,
        TimeOnly closingTime)
    {
        Name = name;
        Description = description;
        Address = address;
        Phone = phone;
        OpeningTime = openingTime;
        ClosingTime = closingTime;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsOpen(TimeOnly? at = null)
    {
        var time = at ?? TimeOnly.FromDateTime(DateTime.Now);
        return OpeningTime <= ClosingTime
            ? time >= OpeningTime && time <= ClosingTime
            : time >= OpeningTime || time <= ClosingTime;
    }
}
