namespace PowerFitness.Api.Models;

public sealed class TelegramRegistrationTicket
{
    public Guid TicketId { get; init; } = Guid.NewGuid();
    public string PhoneNumber { get; set; } = string.Empty;
    public string DeepLink { get; set; } = string.Empty;
    public string Status { get; set; } = "waiting_for_telegram";
    public Guid? UserId { get; set; }
    public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddMinutes(10);
}
