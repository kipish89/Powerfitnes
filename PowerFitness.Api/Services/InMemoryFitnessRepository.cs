using Microsoft.Extensions.Options;
using PowerFitness.Api.Configuration;
using PowerFitness.Api.Models;

namespace PowerFitness.Api.Services;

public sealed class InMemoryFitnessRepository(IOptions<TelegramBotOptions> telegramBotOptions) : IFitnessRepository
{
    private readonly List<UserProfile> _users =
    [
        DefaultSeedData.DemoUser,
        DefaultSeedData.PrimaryTrainerUser
    ];

    private readonly List<TelegramRegistrationTicket> _tickets = [];
    private readonly List<MembershipPlan> _plans = DefaultSeedData.MembershipPlans.ToList();
    private readonly List<TrainerProfile> _trainers = DefaultSeedData.Trainers.ToList();
    private readonly List<WorkoutProgram> _programs = DefaultSeedData.WorkoutPrograms.ToList();
    private readonly List<PaymentRecord> _payments = [DefaultSeedData.DemoPayment];
    private readonly List<ProSubscription> _proSubscriptions = [DefaultSeedData.DemoProSubscription];

    public Task<TelegramRegistrationTicket> StartPhoneRegistrationAsync(string phoneNumber, CancellationToken cancellationToken)
    {
        phoneNumber = PhoneNumberNormalizer.Normalize(phoneNumber);
        var ticket = new TelegramRegistrationTicket
        {
            PhoneNumber = phoneNumber,
            DeepLink = $"https://t.me/{telegramBotOptions.Value.Username}?start=register_{Guid.NewGuid():N}"
        };

        ticket.DeepLink = $"https://t.me/{telegramBotOptions.Value.Username}?start=register_{ticket.TicketId:N}";
        _tickets.Add(ticket);
        return Task.FromResult(ticket);
    }

    public Task<TelegramRegistrationTicket?> GetTicketAsync(Guid ticketId, CancellationToken cancellationToken)
        => Task.FromResult(_tickets.FirstOrDefault(x => x.TicketId == ticketId));

    public Task<UserProfile> ConfirmTelegramRegistrationAsync(TelegramConfirmationRequest request, CancellationToken cancellationToken)
    {
        request.PhoneNumber = PhoneNumberNormalizer.Normalize(request.PhoneNumber);
        var existing = _users.FirstOrDefault(x => x.PhoneNumber == request.PhoneNumber);
        if (existing is not null)
        {
            existing.TelegramConfirmed = true;
            existing.TelegramChatId = request.TelegramChatId;
            existing.FirstName = request.FirstName;
            existing.LastName = request.LastName;
            PromoteTrainerIfNeeded(existing);
            MarkTicketConfirmed(request.TicketId, existing.Id);
            return Task.FromResult(existing);
        }

        var user = new UserProfile
        {
            PhoneNumber = request.PhoneNumber,
            FirstName = request.FirstName,
            LastName = request.LastName,
            TelegramChatId = request.TelegramChatId,
            TelegramConfirmed = true
        };

        PromoteTrainerIfNeeded(user);
        _users.Add(user);
        MarkTicketConfirmed(request.TicketId, user.Id);
        return Task.FromResult(user);
    }

    public Task<UserProfile?> FindUserByPhoneAsync(string phoneNumber, CancellationToken cancellationToken)
    {
        phoneNumber = PhoneNumberNormalizer.Normalize(phoneNumber);
        return Task.FromResult(_users.FirstOrDefault(x => x.PhoneNumber == phoneNumber));
    }

    public Task<DashboardResponse> GetDashboardAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = _users.FirstOrDefault(x => x.Id == userId);
        var trainerProfile = user is null
            ? null
            : _trainers.FirstOrDefault(x => x.UserId == user.Id || x.Id == user.TrainerProfileId);

        return Task.FromResult(new DashboardResponse
        {
            User = user,
            TrainerProfile = trainerProfile,
            MembershipPlans = _plans.Where(x => x.Code.StartsWith("gym", StringComparison.OrdinalIgnoreCase)).ToList(),
            WorkoutPrograms = _programs.OrderByDescending(x => x.UpdatedAtUtc).ToList(),
            Trainers = _trainers.ToList(),
            Payments = _payments.Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAtUtc).ToList(),
            ProSubscription = _proSubscriptions.FirstOrDefault(x => x.UserId == userId)
        });
    }

    public Task<IReadOnlyList<MembershipPlan>> GetMembershipPlansAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<MembershipPlan>>(_plans.ToList());

    public Task<IReadOnlyList<WorkoutProgram>> GetWorkoutProgramsAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<WorkoutProgram>>(_programs.OrderByDescending(x => x.UpdatedAtUtc).ToList());

    public Task<IReadOnlyList<TrainerProfile>> GetTrainersAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<TrainerProfile>>(_trainers.ToList());

    public Task<TrainerProfile?> GetTrainerByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        => Task.FromResult(_trainers.FirstOrDefault(x => x.UserId == userId));

    public Task<WorkoutProgram> SaveTrainerProgramAsync(TrainerProgramUpsertRequest request, CancellationToken cancellationToken)
    {
        var user = _users.FirstOrDefault(x => x.Id == request.UserId);
        if (user is null || !user.IsTrainer)
        {
            throw new InvalidOperationException("Только тренер может управлять программами.");
        }

        var trainer = _trainers.FirstOrDefault(x => x.UserId == user.Id)
            ?? throw new InvalidOperationException("Профиль тренера не найден.");

        WorkoutProgram program;
        if (request.ProgramId.HasValue)
        {
            program = _programs.FirstOrDefault(x => x.Id == request.ProgramId.Value && x.TrainerId == trainer.Id)
                ?? throw new InvalidOperationException("Программа не найдена.");
        }
        else
        {
            program = new WorkoutProgram
            {
                TrainerId = trainer.Id,
                CreatedAtUtc = DateTime.UtcNow
            };
            _programs.Add(program);
        }

        program.Title = request.Title;
        program.Description = request.Description;
        program.Difficulty = request.Difficulty;
        program.DurationMinutes = request.DurationMinutes;
        program.ProOnly = request.ProOnly;
        program.TrainerName = $"{trainer.FirstName} {trainer.LastName}".Trim();
        program.UpdatedAtUtc = DateTime.UtcNow;

        return Task.FromResult(program);
    }

    public Task<PaymentRecord> CreatePurchaseIntentAsync(PurchaseRequest request, CancellationToken cancellationToken)
    {
        if (_users.All(x => x.Id != request.UserId))
        {
            throw new InvalidOperationException("User not found.");
        }

        var plan = _plans.FirstOrDefault(x => x.Code == request.ProductCode);
        if (plan is null)
        {
            throw new InvalidOperationException("Unknown product code.");
        }

        var payment = new PaymentRecord
        {
            UserId = request.UserId,
            ProductCode = request.ProductCode,
            ProductType = request.ProductType,
            Amount = plan.Price,
            Currency = "RUB",
            Status = "awaiting_payment",
            MembershipPlanId = request.ProductType == "membership" ? plan.Id : null,
            ProSubscriptionId = request.ProductType == "pro"
                ? _proSubscriptions.FirstOrDefault(x => x.UserId == request.UserId)?.Id
                : null
        };

        _payments.Add(payment);
        return Task.FromResult(payment);
    }

    public Task<PaymentRecord?> GetPaymentIntentAsync(Guid paymentId, CancellationToken cancellationToken)
        => Task.FromResult(_payments.FirstOrDefault(x => x.Id == paymentId));

    public Task<PaymentRecord> ConfirmTelegramPaymentAsync(TelegramPaymentConfirmationRequest request, CancellationToken cancellationToken)
    {
        var payment = request.PaymentId.HasValue
            ? _payments.LastOrDefault(x => x.Id == request.PaymentId.Value)
            : null;

        payment ??= _payments
            .Where(x => x.UserId == request.UserId &&
                        x.ProductCode == request.ProductCode &&
                        x.ProductType == request.ProductType)
            .LastOrDefault(x => x.Status == "awaiting_payment");

        payment ??= new PaymentRecord
        {
            UserId = request.UserId,
            ProductCode = request.ProductCode,
            ProductType = request.ProductType
        };

        payment.Amount = request.Amount;
        payment.Currency = request.Currency;
        payment.Status = "paid";
        payment.Source = "telegram-bot";

        if (!_payments.Contains(payment))
        {
            _payments.Add(payment);
        }

        if (request.ProductType == "pro")
        {
            var active = _proSubscriptions.FirstOrDefault(x => x.UserId == request.UserId);
            if (active is null)
            {
                active = new ProSubscription
                {
                    UserId = request.UserId,
                    StartsAtUtc = DateTime.UtcNow,
                    EndsAtUtc = DateTime.UtcNow.AddMonths(1),
                    Status = "active"
                };
                _proSubscriptions.Add(active);
            }
            else
            {
                active.EndsAtUtc = active.EndsAtUtc < DateTime.UtcNow
                    ? DateTime.UtcNow.AddMonths(1)
                    : active.EndsAtUtc.AddMonths(1);
                active.Status = "active";
            }

            var user = _users.FirstOrDefault(x => x.Id == request.UserId);
            if (user is not null)
            {
                user.IsProActive = true;
            }
        }

        return Task.FromResult(payment);
    }

    private void MarkTicketConfirmed(Guid ticketId, Guid userId)
    {
        var ticket = _tickets.FirstOrDefault(x => x.TicketId == ticketId);
        if (ticket is null)
        {
            return;
        }

        ticket.Status = "confirmed";
        ticket.UserId = userId;
    }

    private void PromoteTrainerIfNeeded(UserProfile user)
    {
        if (!string.Equals(user.PhoneNumber, DefaultSeedData.PrimaryTrainerPhone, StringComparison.Ordinal))
        {
            return;
        }

        var trainer = _trainers.FirstOrDefault(x => x.PhoneNumber == user.PhoneNumber || x.UserId == user.Id);
        if (trainer is null)
        {
            trainer = new TrainerProfile
            {
                UserId = user.Id,
                PhoneNumber = user.PhoneNumber,
                TelegramChatId = user.TelegramChatId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Bio = "Основной тренерский аккаунт проекта.",
                Specialization = "Силовой тренинг",
                AvatarUrl = user.AvatarUrl,
                CanManagePrograms = true
            };
            _trainers.Add(trainer);
        }
        else
        {
            trainer.UserId = user.Id;
            trainer.PhoneNumber = user.PhoneNumber;
            trainer.TelegramChatId = user.TelegramChatId;
            trainer.FirstName = user.FirstName;
            trainer.LastName = user.LastName;
            trainer.AvatarUrl = user.AvatarUrl;
            trainer.CanManagePrograms = true;
        }

        user.IsTrainer = true;
        user.TrainerProfileId = trainer.Id;
    }
}
