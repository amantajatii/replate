namespace Replate.Domain.Entities;

public sealed class ProductionRecommendation
{
    public Guid MenuId { get; set; }
    public string MenuName { get; set; } = string.Empty;
    public int TypicalProduction { get; set; }
    public decimal AverageDemand { get; set; }
    public decimal SafetyBuffer { get; set; }
    public int RecommendedProduction { get; private set; }
    public int PotentialSurplusReduction { get; private set; }

    public decimal CalculateAverageDemand() => AverageDemand;

    public int CalculateRecommendedProduction()
    {
        RecommendedProduction = Math.Max(0, (int)Math.Round(AverageDemand + SafetyBuffer));
        return RecommendedProduction;
    }

    public int CalculatePotentialReduction()
    {
        PotentialSurplusReduction = Math.Max(0, TypicalProduction - CalculateRecommendedProduction());
        return PotentialSurplusReduction;
    }
}
