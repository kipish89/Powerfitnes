using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PowerFitness.Api.Data;
using PowerFitness.Api.Models;

namespace PowerFitness.Api.Services;

public sealed class AuthService(
    PowerFitnessDbContext dbContext,
    IJwtTokenService jwtTokenService) : IAuthService
{
    private readonly PasswordHasher<UserProfile> _passwordHasher = new();

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        request.PhoneNumber = PhoneNumberNormalizer.Normalize(request.PhoneNumber);
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            throw new InvalidOperationException("Phone number is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        {
            throw new InvalidOperationException("Password must contain at least 6 characters.");
        }

        var exists = await dbContext.Users.AnyAsync(x => x.PhoneNumber == request.PhoneNumber, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("User with this phone already exists.");
        }

        var user = new UserProfile
        {
            PhoneNumber = request.PhoneNumber,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return jwtTokenService.CreateToken(user);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        request.PhoneNumber = PhoneNumberNormalizer.Normalize(request.PhoneNumber);
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.PhoneNumber == request.PhoneNumber, cancellationToken);
        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return null;
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        return jwtTokenService.CreateToken(user);
    }

    public async Task<AuthResponse?> ExchangeTelegramTicketAsync(Guid ticketId, CancellationToken cancellationToken)
    {
        var ticket = await dbContext.RegistrationTickets.FirstOrDefaultAsync(x => x.TicketId == ticketId, cancellationToken);
        if (ticket is null ||
            ticket.ExpiresAtUtc <= DateTime.UtcNow ||
            !string.Equals(ticket.Status, "confirmed", StringComparison.OrdinalIgnoreCase) ||
            !ticket.UserId.HasValue)
        {
            return null;
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == ticket.UserId.Value, cancellationToken);
        return user is null ? null : jwtTokenService.CreateToken(user);
    }
}
