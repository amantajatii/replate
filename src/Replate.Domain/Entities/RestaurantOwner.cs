using Replate.Domain.Enums;
using Replate.Domain.Services;

namespace Replate.Domain.Entities;

public sealed class RestaurantOwner : User
{
    public override UserRole Role => UserRole.RestaurantOwner;
    public List<Restaurant> Restaurants { get; } = [];

    public Restaurant ManageRestaurant(Restaurant restaurant)
    {
        RequireAuthentication();
        if (restaurant.OwnerId != Guid.Empty)
        {
            RequireOwnedRestaurant(restaurant);
        }

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
        RequireOwnedRestaurant(restaurant);
        if ((menu.RestaurantId != Guid.Empty && menu.RestaurantId != restaurant.Id) ||
            (menu.Restaurant is not null && menu.Restaurant.Id != restaurant.Id))
        {
            throw new UnauthorizedAccessException("Menu ini milik restoran lain.");
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
        RequireOwnedMenu(menu);
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

    public IReadOnlyList<Order> ManageOrders(Restaurant restaurant)
    {
        RequireOwnedRestaurant(restaurant);
        return restaurant.Orders.Where(order => order.RestaurantId == restaurant.Id).ToList();
    }

    public ProductionRecord RecordProduction(Menu menu, ProductionRecord record)
    {
        RequireOwnedMenu(menu);
        if (record.MenuId != Guid.Empty && record.MenuId != menu.Id)
        {
            throw new UnauthorizedAccessException("Catatan produksi ini milik menu lain.");
        }

        record.Menu = menu;
        record.MenuId = menu.Id;
        menu.ProductionRecords.Add(record);
        return record;
    }

    public SurplusDashboard ViewDashboard()
    {
        RequireAuthentication();
        return new();
    }

    private void RequireOwnedRestaurant(Restaurant restaurant)
    {
        RequireAuthentication();
        if (restaurant.OwnerId != Id)
        {
            throw new UnauthorizedAccessException("Anda tidak memiliki akses ke restoran ini.");
        }
    }

    private void RequireOwnedMenu(Menu menu)
    {
        if (menu.Restaurant is not { } restaurant || menu.RestaurantId != restaurant.Id)
        {
            throw new UnauthorizedAccessException("Restoran pemilik menu tidak ditemukan.");
        }

        RequireOwnedRestaurant(restaurant);
    }
}
