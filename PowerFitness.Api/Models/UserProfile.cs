using System.Text.Json.Serialization;

namespace PowerFitness.Api.Models;

public sealed class UserProfile
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string PhoneNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public string PasswordHash { get; set; } = string.Empty;
    public bool TelegramConfirmed { get; set; }
    public string TelegramChatId { get; set; } = string.Empty;
    public bool IsProActive { get; set; }
    public bool IsTrainer { get; set; }
    public Guid? TrainerProfileId { get; set; }

    [JsonIgnore]
    public ICollection<PaymentRecord> Payments { get; set; } = [];

    [JsonIgnore]
    public ICollection<ProSubscription> ProSubscriptions { get; set; } = [];
}
