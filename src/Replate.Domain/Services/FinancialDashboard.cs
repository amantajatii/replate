using Replate.Domain.Entities;
using Replate.Domain.Enums;

namespace Replate.Domain.Services;

public sealed class FinancialDashboard
{
    public decimal CalculateRecoveredRevenue(IEnumerable<Order> orders) =>
        orders.Where(order => order.Status == OrderStatus.Completed).Sum(order => order.TotalPrice);

    public decimal CalculateEstimatedLoss(IEnumerable<ProductionRecord> records) =>
        records.Sum(record => record.CalculateWasted() * (record.Menu?.NormalPrice ?? 0));

    public decimal CalculateSurplusSalesRevenue(IEnumerable<Order> orders) => CalculateRecoveredRevenue(orders);

    public decimal CalculateDiscountValue(IEnumerable<Order> orders) =>
        orders.Where(order => order.Status == OrderStatus.Completed)
            .SelectMany(order => order.Items)
            .Sum(item => (item.Listing.Menu.NormalPrice - item.UnitPrice) * item.Quantity);
}
