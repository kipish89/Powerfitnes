namespace PowerFitness.Api.Models;

public sealed class TelegramPaymentConfirmationRequest
{
    public Guid? PaymentId { get; set; }
    public Guid UserId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "RUB";
}
