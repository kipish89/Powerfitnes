using PowerFitness.Api.Models;

namespace PowerFitness.Api.Services;

public interface ITelegramRegistrationService
{
    Task SendRegistrationInviteAsync(TelegramRegistrationTicket ticket, CancellationToken cancellationToken);
    Task SendPurchaseInvoiceAsync(PurchaseRequest request, PaymentRecord payment, CancellationToken cancellationToken);
    string BuildPurchaseDeepLink(PaymentRecord payment);
    string GetBotUsername();
}
