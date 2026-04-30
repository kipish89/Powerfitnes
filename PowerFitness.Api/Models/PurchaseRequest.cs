namespace PowerFitness.Api.Models;

public sealed class PurchaseRequest
{
    public Guid UserId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
}
