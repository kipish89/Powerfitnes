namespace PowerFitness.Api.Models;

public sealed class MembershipPlan
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int DurationMonths { get; set; }
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
}
