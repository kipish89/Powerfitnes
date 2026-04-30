using PowerFitness.Api.Models;

namespace PowerFitness.Api.Services;

public static class DefaultSeedData
{
    public const string PrimaryTrainerPhone = "+79385317843";

    public static readonly UserProfile DemoUser = new()
    {
        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        PhoneNumber = "+79990000001",
        FirstName = "Иван",
        LastName = "Петров",
        Gender = "Мужской",
        TelegramConfirmed = true,
        TelegramChatId = "123456789",
        IsProActive = true,
        AvatarUrl = "https://placehold.co/160x160"
    };

    public static readonly UserProfile PrimaryTrainerUser = new()
    {
        Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        PhoneNumber = PrimaryTrainerPhone,
        FirstName = "Тренер",
        LastName = "PowerFitness",
        Gender = "Не указан",
        TelegramConfirmed = false,
        TelegramChatId = string.Empty,
        IsTrainer = true,
        TrainerProfileId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
        AvatarUrl = "https://placehold.co/160x160"
    };

    public static readonly TrainerProfile PrimaryTrainerProfile = new()
    {
        Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
        UserId = PrimaryTrainerUser.Id,
        PhoneNumber = PrimaryTrainerPhone,
        TelegramChatId = string.Empty,
        FirstName = "Тренер",
        LastName = "PowerFitness",
        Bio = "Основной тренерский аккаунт проекта.",
        Specialization = "Силовой тренинг",
        AvatarUrl = "https://placehold.co/120x120",
        CanManagePrograms = true
    };

    public static readonly PaymentRecord DemoPayment = new()
    {
        UserId = DemoUser.Id,
        ProductCode = "pro-1m",
        ProductType = "pro",
        Amount = 990,
        Currency = "RUB",
        Status = "paid",
        Source = "telegram-bot"
    };

    public static readonly ProSubscription DemoProSubscription = new()
    {
        UserId = DemoUser.Id,
        StartsAtUtc = DateTime.UtcNow.AddDays(-10),
        EndsAtUtc = DateTime.UtcNow.AddDays(20),
        Status = "active"
    };

    public static readonly IReadOnlyList<MembershipPlan> MembershipPlans =
    [
        new() { Code = "gym-3m", Title = "Абонемент на 3 месяца", DurationMonths = 3, Price = 4500, Description = "Базовый доступ в тренажерный зал." },
        new() { Code = "gym-6m", Title = "Абонемент на 6 месяцев", DurationMonths = 6, Price = 7800, Description = "Оптимальный вариант для стабильных тренировок." },
        new() { Code = "gym-12m", Title = "Абонемент на 12 месяцев", DurationMonths = 12, Price = 12900, Description = "Максимально выгодный годовой доступ." },
        new() { Code = "pro-1m", Title = "PowerFitness Pro", DurationMonths = 1, Price = 990, Description = "Доступ к закрытым программам и расширенной статистике." }
    ];

    public static readonly IReadOnlyList<TrainerProfile> Trainers =
    [
        PrimaryTrainerProfile,
        new()
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            FirstName = "Елена",
            LastName = "Миронова",
            Bio = "Функциональные тренировки, сушка и выносливость.",
            Specialization = "Функциональный тренинг",
            AvatarUrl = "https://placehold.co/120x120",
            CanManagePrograms = false
        }
    ];

    public static readonly IReadOnlyList<WorkoutProgram> WorkoutPrograms =
    [
        new()
        {
            Title = "Старт в зале",
            Description = "Базовая программа на 4 недели.",
            Difficulty = "Новичок",
            DurationMinutes = 60,
            ProOnly = false,
            TrainerId = PrimaryTrainerProfile.Id,
            TrainerName = "Тренер PowerFitness"
        },
        new()
        {
            Title = "Upper/Lower Pro",
            Description = "Продвинутый сплит с контролем прогрессии.",
            Difficulty = "Продвинутый",
            DurationMinutes = 75,
            ProOnly = true,
            TrainerId = PrimaryTrainerProfile.Id,
            TrainerName = "Тренер PowerFitness"
        },
        new()
        {
            Title = "Сушка 30 дней",
            Description = "Функциональная программа с прогресс-трекингом.",
            Difficulty = "Средний",
            DurationMinutes = 45,
            ProOnly = true,
            TrainerId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            TrainerName = "Елена Миронова"
        }
    ];
}
