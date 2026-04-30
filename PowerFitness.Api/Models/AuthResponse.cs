namespace PowerFitness.Api.Models;

public sealed class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public UserProfile User { get; set; } = new();
}
