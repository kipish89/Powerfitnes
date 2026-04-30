namespace PowerFitness.App.Models;

public sealed class UserProfileVm
{
    public Guid Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = "https://placehold.co/160x160";
    public bool TelegramConfirmed { get; set; }
    public bool IsProActive { get; set; }
    public bool IsTrainer { get; set; }
    public Guid? TrainerProfileId { get; set; }
}

public sealed class MembershipPlanVm
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int DurationMonths { get; set; }
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
}

public sealed class WorkoutProgramVm
{
    public Guid Id { get; set; }
    public Guid? TrainerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public bool ProOnly { get; set; }
    public string TrainerName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class TrainerProfileVm
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string TelegramChatId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = "https://placehold.co/120x120";
    public bool CanManagePrograms { get; set; }
}

public sealed class PaymentRecordVm
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "RUB";
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class ProSubscriptionVm
{
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class DashboardVm
{
    public UserProfileVm? User { get; set; }
    public TrainerProfileVm? TrainerProfile { get; set; }
    public IReadOnlyList<MembershipPlanVm> MembershipPlans { get; set; } = [];
    public IReadOnlyList<WorkoutProgramVm> WorkoutPrograms { get; set; } = [];
    public IReadOnlyList<TrainerProfileVm> Trainers { get; set; } = [];
    public IReadOnlyList<PaymentRecordVm> Payments { get; set; } = [];
    public ProSubscriptionVm? ProSubscription { get; set; }
}

public sealed class RegistrationStartResult
{
    public Guid TicketId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string DeepLink { get; set; } = string.Empty;
}

public sealed class RegisterRequestVm
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public sealed class LoginRequestVm
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class AuthResponseVm
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public UserProfileVm User { get; set; } = new();
}

public sealed class ApiCallResult<T>
{
    public bool Success { get; set; }
    public bool IsUnauthorized { get; set; }
    public bool IsNotFound { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
}

public sealed class RegistrationStatusResult
{
    public Guid TicketId { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}

public sealed class PurchaseStartResult
{
    public string Status { get; set; } = string.Empty;
    public string DeepLink { get; set; } = string.Empty;
}

public sealed class FileUploadResultVm
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
}

public sealed class TrainerProgramUpsertRequestVm
{
    public Guid UserId { get; set; }
    public Guid? ProgramId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public bool ProOnly { get; set; }
}
