using System.Security.Cryptography;
using Replate.Domain.Entities;

namespace Replate.Domain.Services;

public sealed class CheckoutService(StockService stockService)
{
    public bool ValidateCart(CartRestaurantGroup group) => group.Validate();

    public bool ValidateStock(CartRestaurantGroup group) =>
        group.Items.All(item => item.Quantity > 0 && item.Quantity <= item.Listing.Quantity);

    public bool ValidatePickupWindow(CartRestaurantGroup group, DateTime? now = null) =>
        group.Items.All(item => item.Listing.IsAvailable(now ?? DateTime.UtcNow));

    public Order CreateOrder(Cart cart, Guid restaurantId)
    {
        var order = PrepareOrder(cart, restaurantId);
        CommitOrder(cart, order);
        return order;
    }

    public Order PrepareOrder(Cart cart, Guid restaurantId)
    {
        var group = cart.FindGroup(restaurantId);
        if (group is null || !ValidateCart(group) || !ValidateStock(group) || !ValidatePickupWindow(group))
        {
            throw new InvalidOperationException("Cart cannot be checked out.");
        }

        var order = new Order
        {
            CustomerId = cart.CustomerId,
            RestaurantId = group.RestaurantId,
            PickupCode = GeneratePickupCode(),
            TotalPrice = group.CalculateSubtotal()
        };

        order.Items.AddRange(group.Items.Select(item => new OrderItem
        {
            OrderId = order.Id,
            ListingId = item.ListingId,
            Listing = item.Listing,
            MenuName = item.Listing.Menu.Name,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice
        }));
        return order;
    }

    public void CommitOrder(Cart cart, Order order)
    {
        var reservedItems = new List<OrderItem>();
        try
        {
            foreach (var item in order.Items)
            {
                stockService.ReserveStock(item.Listing, item.Quantity);
                reservedItems.Add(item);
            }
        }
        catch
        {
            foreach (var item in reservedItems)
            {
                stockService.RestoreStock(item.Listing, item.Quantity);
            }

            throw;
        }

        foreach (var item in order.Items)
        {
            cart.RemoveItem(item.ListingId);
        }
    }

    public string GeneratePickupCode() =>
        RandomNumberGenerator.GetString("ABCDEFGHJKLMNPQRSTUVWXYZ23456789", 6);
}
