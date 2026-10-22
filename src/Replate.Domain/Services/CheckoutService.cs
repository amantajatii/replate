using System.Security.Cryptography;
using Replate.Domain.Entities;

namespace Replate.Domain.Services;

public sealed class CheckoutService(StockService stockService)
{
    public bool ValidateCart(Cart cart) => cart.ValidateCart();

    public bool ValidateStock(Cart cart) =>
        cart.Items.All(item => item.Quantity > 0 && item.Quantity <= item.Listing.Quantity);

    public bool ValidatePickupWindow(Cart cart, DateTime? now = null) =>
        cart.Items.All(item => item.Listing.IsAvailable(now ?? DateTime.UtcNow));

    public Order CreateOrder(Cart cart)
    {
        if (!ValidateCart(cart) || !ValidateStock(cart) || !ValidatePickupWindow(cart))
        {
            throw new InvalidOperationException("Cart cannot be checked out.");
        }

        var reservedItems = new List<CartItem>();
        try
        {
            foreach (var item in cart.Items)
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

        var order = new Order
        {
            CustomerId = cart.CustomerId,
            RestaurantId = cart.RestaurantId,
            PickupCode = GeneratePickupCode(),
            TotalPrice = cart.CalculateSubtotal()
        };

        order.Items.AddRange(cart.Items.Select(item => new OrderItem
        {
            OrderId = order.Id,
            ListingId = item.ListingId,
            Listing = item.Listing,
            MenuName = item.Listing.Menu.Name,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice
        }));

        cart.ClearCart();
        return order;
    }

    public string GeneratePickupCode() =>
        RandomNumberGenerator.GetString("ABCDEFGHJKLMNPQRSTUVWXYZ23456789", 6);
}
