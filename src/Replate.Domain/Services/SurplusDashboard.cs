using Replate.Domain.Entities;

namespace Replate.Domain.Services;

public sealed class SurplusDashboard
{
    public int CalculateTotalProduced(IEnumerable<ProductionRecord> records) => records.Sum(record => record.ProducedQuantity);

    public int CalculateTotalSurplus(IEnumerable<ProductionRecord> records) => records.Sum(record => record.CalculateSurplus());

    public decimal CalculateSurplusRate(IEnumerable<ProductionRecord> records)
    {
        var materialized = records.ToList();
        var produced = CalculateTotalProduced(materialized);
        return produced == 0 ? 0 : (decimal)CalculateTotalSurplus(materialized) / produced * 100;
    }

    public int CalculateRescuedFood(IEnumerable<ProductionRecord> records) => records.Sum(record => record.RescuedQuantity);

    public int CalculateWastedFood(IEnumerable<ProductionRecord> records) => records.Sum(record => record.CalculateWasted());

    public decimal CalculateRescueRate(IEnumerable<ProductionRecord> records)
    {
        var materialized = records.ToList();
        var surplus = CalculateTotalSurplus(materialized);
        return surplus == 0 ? 0 : (decimal)CalculateRescuedFood(materialized) / surplus * 100;
    }

    public decimal CalculateRescuedWeight(IEnumerable<ProductionRecord> records) =>
        records.Sum(record => record.RescuedQuantity * (record.Menu?.WeightPerPortion ?? 0));

    public Menu? GetHighestSurplusMenu(IEnumerable<Menu> menus) =>
        menus.MaxBy(menu => menu.ProductionRecords.Sum(record => record.CalculateSurplus()));

    public IReadOnlyDictionary<DateOnly, int> GetSurplusTrend(IEnumerable<ProductionRecord> records) =>
        records.GroupBy(record => record.RecordDate)
            .ToDictionary(group => group.Key, group => group.Sum(record => record.CalculateSurplus()));
}
