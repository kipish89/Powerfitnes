using Microsoft.AspNetCore.Mvc;
using PowerFitness.Api.Models;
using PowerFitness.Api.Services;

namespace PowerFitness.Api.Controllers;

[ApiController]
[Route("api/bot")]
public sealed class BotController(IFitnessRepository repository) : ControllerBase
{
    [HttpPost("payment-confirmation")]
    public async Task<IActionResult> ConfirmPayment(
        [FromBody] TelegramPaymentConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var payment = await repository.ConfirmTelegramPaymentAsync(request, cancellationToken);
            return Ok(payment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
