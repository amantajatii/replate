namespace Replate.Domain.Entities;

public sealed class ProductionRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid MenuId { get; set; }
    public Menu? Menu { get; set; }
    public DateOnly RecordDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public int ProducedQuantity { get; set; }
    public int RegularSoldQuantity { get; set; }
    public int SurplusQuantity { get; private set; }
    public int RescuedQuantity { get; set; }
    public int WastedQuantity { get; private set; }

    public int CalculateSurplus() => Math.Max(0, ProducedQuantity - RegularSoldQuantity);

    public int CalculateWasted() => Math.Max(0, CalculateSurplus() - RescuedQuantity);

    public void UpdateRescuedQuantity(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(quantity);
        if (quantity > CalculateSurplus())
        {
            throw new InvalidOperationException("Rescued quantity exceeds surplus.");
        }

        RescuedQuantity = quantity;
        SurplusQuantity = CalculateSurplus();
        WastedQuantity = CalculateWasted();
    }
}
