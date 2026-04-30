using Microsoft.AspNetCore.Mvc;
using PowerFitness.Api.Models;
using PowerFitness.Api.Services;

namespace PowerFitness.Api.Controllers;

[ApiController]
[Route("api/purchases")]
public sealed class PurchasesController(
    IFitnessRepository repository,
    ITelegramRegistrationService telegramRegistrationService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreatePurchase([FromBody] PurchaseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await repository.CreatePurchaseIntentAsync(request, cancellationToken);
            await telegramRegistrationService.SendPurchaseInvoiceAsync(request, payment, cancellationToken);
            var deepLink = telegramRegistrationService.BuildPurchaseDeepLink(payment);

            return Ok(new
            {
                payment.Id,
                payment.Status,
                deepLink,
                message = "Purchase created. Open Telegram bot to continue."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{paymentId:guid}")]
    public async Task<IActionResult> GetPurchase(Guid paymentId, CancellationToken cancellationToken)
    {
        var payment = await repository.GetPaymentIntentAsync(paymentId, cancellationToken);
        return payment is null ? NotFound() : Ok(payment);
    }
}
