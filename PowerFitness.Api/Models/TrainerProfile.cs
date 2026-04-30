namespace PowerFitness.Api.Models;

public sealed class TrainerProfile
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string TelegramChatId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public bool CanManagePrograms { get; set; } = true;
}
