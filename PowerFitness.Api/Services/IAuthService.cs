using PowerFitness.Api.Models;

namespace PowerFitness.Api.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthResponse?> ExchangeTelegramTicketAsync(Guid ticketId, CancellationToken cancellationToken);
}
