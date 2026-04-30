namespace PowerFitness.Api.Models;

public sealed class PaymentRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public UserProfile? User { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public Guid? MembershipPlanId { get; set; }
    public Guid? ProSubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "RUB";
    public string Status { get; set; } = "pending";
    public string Source { get; set; } = "telegram-bot";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
