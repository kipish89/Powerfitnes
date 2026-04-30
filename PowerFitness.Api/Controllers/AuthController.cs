using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PowerFitness.Api.Models;
using PowerFitness.Api.Services;

namespace PowerFitness.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IFitnessRepository repository,
    ITelegramRegistrationService telegramRegistrationService,
    IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await authService.RegisterAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        return result is null ? Unauthorized(new { message = "Invalid phone or password." }) : Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserProfile>> Me(CancellationToken cancellationToken)
    {
        var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(rawUserId, out var userId))
        {
            return Unauthorized();
        }

        var dashboard = await repository.GetDashboardAsync(userId, cancellationToken);
        return dashboard.User is null ? NotFound() : Ok(dashboard.User);
    }

    [HttpPost("start")]
    public async Task<ActionResult<TelegramRegistrationTicket>> StartRegistration(
        [FromBody] PhoneStartRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedPhone = PhoneNumberNormalizer.Normalize(request.PhoneNumber);
        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            return BadRequest(new { message = "Phone number is required." });
        }

        var ticket = await repository.StartPhoneRegistrationAsync(normalizedPhone, cancellationToken);
        await telegramRegistrationService.SendRegistrationInviteAsync(ticket, cancellationToken);
        return Ok(ticket);
    }

    [HttpGet("status/{ticketId:guid}")]
    public async Task<ActionResult<object>> GetStatus(Guid ticketId, CancellationToken cancellationToken)
    {
        var ticket = await repository.GetTicketAsync(ticketId, cancellationToken);
        if (ticket is null)
        {
            return NotFound();
        }

        return Ok(new
        {
            ticket.TicketId,
            ticket.Status,
            ticket.UserId,
            ticket.ExpiresAtUtc
        });
    }

    [HttpPost("telegram/exchange/{ticketId:guid}")]
    public async Task<ActionResult<AuthResponse>> ExchangeTelegramTicket(Guid ticketId, CancellationToken cancellationToken)
    {
        var result = await authService.ExchangeTelegramTicketAsync(ticketId, cancellationToken);
        return result is null ? Unauthorized(new { message = "Ticket is invalid or expired." }) : Ok(result);
    }

    [HttpPost("telegram/confirm")]
    public async Task<ActionResult<UserProfile>> ConfirmFromTelegram(
        [FromBody] TelegramConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        request.PhoneNumber = PhoneNumberNormalizer.Normalize(request.PhoneNumber);
        var user = await repository.ConfirmTelegramRegistrationAsync(request, cancellationToken);
        return Ok(user);
    }

    [HttpGet("user-by-phone")]
    public async Task<ActionResult<UserProfile>> GetByPhone([FromQuery] string phoneNumber, CancellationToken cancellationToken)
    {
        var user = await repository.FindUserByPhoneAsync(PhoneNumberNormalizer.Normalize(phoneNumber), cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }
}
