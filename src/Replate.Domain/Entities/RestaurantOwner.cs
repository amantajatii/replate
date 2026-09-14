using Replate.Domain.Enums;
using Replate.Domain.Services;

namespace Replate.Domain.Entities;

public sealed class RestaurantOwner : User
{
    public override UserRole Role => UserRole.RestaurantOwner;
    public List<Restaurant> Restaurants { get; } = [];

    public Restaurant ManageRestaurant(Restaurant restaurant)
    {
        restaurant.Owner = this;
        restaurant.OwnerId = Id;
        if (!Restaurants.Contains(restaurant))
        {
            Restaurants.Add(restaurant);
        }

        return restaurant;
    }

    public Menu ManageMenu(Restaurant restaurant, Menu menu)
    {
        if (!Restaurants.Contains(restaurant))
        {
            throw new InvalidOperationException("Restaurant is not owned by this user.");
        }

        menu.Restaurant = restaurant;
        menu.RestaurantId = restaurant.Id;
        if (!restaurant.Menus.Contains(menu))
        {
            restaurant.Menus.Add(menu);
        }

        return menu;
    }

    public SurplusListing CreateSurplusListing(
        Menu menu,
        decimal rescuePrice,
        int quantity,
        DateTime pickupStart,
        DateTime pickupEnd)
    {
        var listing = new SurplusListing
        {
            MenuId = menu.Id,
            Menu = menu,
            RescuePrice = rescuePrice,
            Quantity = quantity,
            PickupStart = pickupStart,
            PickupEnd = pickupEnd
        };
        menu.SurplusListings.Add(listing);
        return listing;
    }

    public IReadOnlyList<Order> ManageOrders(Restaurant restaurant) => restaurant.Orders;

    public ProductionRecord RecordProduction(Menu menu, ProductionRecord record)
    {
        record.Menu = menu;
        record.MenuId = menu.Id;
        menu.ProductionRecords.Add(record);
        return record;
    }

    public SurplusDashboard ViewDashboard() => new();
}
