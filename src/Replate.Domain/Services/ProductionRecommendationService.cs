using Replate.Domain.Entities;

namespace Replate.Domain.Services;

public sealed class ProductionRecommendationService
{
    public IEnumerable<ProductionRecord> GetHistoricalDemand(Menu menu, DayOfWeek dayOfWeek) =>
        menu.ProductionRecords.Where(record => record.RecordDate.DayOfWeek == dayOfWeek);

    public decimal CalculateAverageDemand(IEnumerable<ProductionRecord> records)
    {
        var demand = records.Select(record => record.RegularSoldQuantity).ToList();
        return demand.Count == 0 ? 0 : (decimal)demand.Sum() / demand.Count;
    }

    public decimal ApplySafetyBuffer(decimal averageDemand, decimal buffer) => averageDemand + buffer;

    public ProductionRecommendation GenerateRecommendation(Menu menu, decimal safetyBuffer = 0)
    {
        var records = menu.ProductionRecords;
        var recommendation = new ProductionRecommendation
        {
            MenuId = menu.Id,
            MenuName = menu.Name,
            TypicalProduction = records.Count == 0 ? 0 : (int)Math.Round(records.Average(record => record.ProducedQuantity)),
            AverageDemand = CalculateAverageDemand(records),
            SafetyBuffer = safetyBuffer
        };

        recommendation.CalculateRecommendedProduction();
        recommendation.CalculatePotentialReduction();
        return recommendation;
    }
}
