using Microsoft.Extensions.Options;
using PowerFitness.Api.Configuration;
using PowerFitness.Api.Models;

namespace PowerFitness.Api.Services;

public sealed class TelegramRegistrationService(
    IHttpClientFactory httpClientFactory,
    IOptions<TelegramBotOptions> telegramBotOptions) : ITelegramRegistrationService
{
    public Task SendRegistrationInviteAsync(TelegramRegistrationTicket ticket, CancellationToken cancellationToken)
    {
        _ = httpClientFactory;
        _ = ticket;
        return Task.CompletedTask;
    }

    public Task SendPurchaseInvoiceAsync(PurchaseRequest request, PaymentRecord payment, CancellationToken cancellationToken)
    {
        _ = httpClientFactory;
        _ = request;
        _ = payment;
        return Task.CompletedTask;
    }

    public string BuildPurchaseDeepLink(PaymentRecord payment)
    {
        var payload = $"buy_{payment.Id:N}";
        return $"https://t.me/{telegramBotOptions.Value.Username}?start={payload}";
    }

    public string GetBotUsername() => telegramBotOptions.Value.Username;
}
