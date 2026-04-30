using PowerFitness.Api.Models;

namespace PowerFitness.Api.Services;

public interface IJwtTokenService
{
    AuthResponse CreateToken(UserProfile user);
}
