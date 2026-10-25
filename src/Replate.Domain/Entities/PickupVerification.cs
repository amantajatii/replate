namespace Replate.Domain.Entities;

public sealed class PickupVerification
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = new();
    public string EnteredCode { get; set; } = string.Empty;
    public DateTime? VerifiedAt { get; private set; }
    public bool IsValid { get; private set; }

    public bool VerifyCode()
    {
        IsValid = string.Equals(EnteredCode, Order.PickupCode, StringComparison.Ordinal);
        VerifiedAt = IsValid ? DateTime.UtcNow : null;
        return IsValid;
    }

    public void CompletePickup()
    {
        if (!IsValid)
        {
            throw new InvalidOperationException("Pickup code has not been verified.");
        }

        Order.Complete();
    }
}
