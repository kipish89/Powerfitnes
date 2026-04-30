namespace PowerFitness.Api.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "PowerFitness.Api";
    public string Audience { get; set; } = "PowerFitness.Client";
    public string SigningKey { get; set; } = "PowerFitness.Super.Secret.Dev.Key.2026";
    public int ExpiresMinutes { get; set; } = 120;
}
