using Replate.Domain.Entities;

namespace Replate.Domain.Services;

public sealed class MarketplaceService(IEnumerable<SurplusListing> listings)
{
    private readonly List<SurplusListing> listings = [.. listings];

    public IEnumerable<SurplusListing> GetActiveListings() =>
        listings.Where(listing => listing.IsAvailable(DateTime.UtcNow));

    public IEnumerable<SurplusListing> SearchByFood(string keyword) =>
        GetActiveListings().Where(listing => listing.Menu.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<SurplusListing> SearchByRestaurant(string keyword) =>
        GetActiveListings().Where(listing =>
            listing.Menu.Restaurant?.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true);

    public IEnumerable<SurplusListing> FilterByCategory(string category) =>
        GetActiveListings().Where(listing => listing.Menu.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<SurplusListing> FilterByPrice(decimal minimumPrice, decimal maximumPrice) =>
        GetActiveListings().Where(listing => listing.RescuePrice >= minimumPrice && listing.RescuePrice <= maximumPrice);

    public IEnumerable<SurplusListing> FilterByPickupTime(DateTime pickupTime) =>
        GetActiveListings().Where(listing => pickupTime >= listing.PickupStart && pickupTime <= listing.PickupEnd);

    public IEnumerable<SurplusListing> FilterAvailableOnly() => GetActiveListings();
}
