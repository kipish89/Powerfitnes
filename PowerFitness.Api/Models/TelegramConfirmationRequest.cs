namespace PowerFitness.Api.Models;

public sealed class TelegramConfirmationRequest
{
    public Guid TicketId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string TelegramChatId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
