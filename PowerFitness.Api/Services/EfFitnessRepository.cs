using Microsoft.EntityFrameworkCore;
using PowerFitness.Api.Data;
using PowerFitness.Api.Models;

namespace PowerFitness.Api.Services;

public sealed class EfFitnessRepository(
    PowerFitnessDbContext dbContext,
    ITelegramRegistrationService telegramRegistrationService) : IFitnessRepository
{
    public async Task<TelegramRegistrationTicket> StartPhoneRegistrationAsync(string phoneNumber, CancellationToken cancellationToken)
    {
        phoneNumber = PhoneNumberNormalizer.Normalize(phoneNumber);
        var ticket = new TelegramRegistrationTicket
        {
            PhoneNumber = phoneNumber,
            DeepLink = $"https://t.me/{telegramRegistrationService.GetBotUsername()}?start=register_{Guid.NewGuid():N}"
        };

        ticket.DeepLink = $"https://t.me/{telegramRegistrationService.GetBotUsername()}?start=register_{ticket.TicketId:N}";
        dbContext.RegistrationTickets.Add(ticket);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ticket;
    }

    public Task<TelegramRegistrationTicket?> GetTicketAsync(Guid ticketId, CancellationToken cancellationToken)
        => dbContext.RegistrationTickets.FirstOrDefaultAsync(x => x.TicketId == ticketId, cancellationToken);

    public async Task<UserProfile> ConfirmTelegramRegistrationAsync(TelegramConfirmationRequest request, CancellationToken cancellationToken)
    {
        request.PhoneNumber = PhoneNumberNormalizer.Normalize(request.PhoneNumber);
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.PhoneNumber == request.PhoneNumber, cancellationToken);
        if (user is null)
        {
            user = new UserProfile
            {
                PhoneNumber = request.PhoneNumber,
                FirstName = request.FirstName,
                LastName = request.LastName
            };

            dbContext.Users.Add(user);
        }
        else
        {
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
        }

        user.TelegramConfirmed = true;
        user.TelegramChatId = request.TelegramChatId;

        await PromoteTrainerIfNeededAsync(user, cancellationToken);

        var ticket = await dbContext.RegistrationTickets.FirstOrDefaultAsync(x => x.TicketId == request.TicketId, cancellationToken);
        if (ticket is not null)
        {
            ticket.Status = "confirmed";
            ticket.UserId = user.Id;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    public Task<UserProfile?> FindUserByPhoneAsync(string phoneNumber, CancellationToken cancellationToken)
    {
        phoneNumber = PhoneNumberNormalizer.Normalize(phoneNumber);
        return dbContext.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber, cancellationToken);
    }

    public async Task<DashboardResponse> GetDashboardAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        var trainers = await dbContext.Trainers.AsNoTracking().ToListAsync(cancellationToken);
        var programs = await dbContext.WorkoutPrograms.AsNoTracking().OrderByDescending(x => x.UpdatedAtUtc).ToListAsync(cancellationToken);
        var payments = await dbContext.Payments.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        var subscription = await dbContext.ProSubscriptions.AsNoTracking()
            .OrderByDescending(x => x.EndsAtUtc)
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        var trainerProfile = user is null
            ? null
            : trainers.FirstOrDefault(x => x.UserId == user.Id || x.Id == user.TrainerProfileId);

        return new DashboardResponse
        {
            User = user,
            TrainerProfile = trainerProfile,
            MembershipPlans = await dbContext.MembershipPlans.AsNoTracking().ToListAsync(cancellationToken),
            WorkoutPrograms = programs,
            Trainers = trainers,
            Payments = payments,
            ProSubscription = subscription
        };
    }

    public async Task<IReadOnlyList<MembershipPlan>> GetMembershipPlansAsync(CancellationToken cancellationToken)
        => await dbContext.MembershipPlans.AsNoTracking().OrderBy(x => x.DurationMonths).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WorkoutProgram>> GetWorkoutProgramsAsync(CancellationToken cancellationToken)
        => await dbContext.WorkoutPrograms.AsNoTracking().OrderByDescending(x => x.UpdatedAtUtc).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TrainerProfile>> GetTrainersAsync(CancellationToken cancellationToken)
        => await dbContext.Trainers.AsNoTracking().OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ToListAsync(cancellationToken);

    public Task<TrainerProfile?> GetTrainerByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        => dbContext.Trainers.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    public async Task<WorkoutProgram> SaveTrainerProgramAsync(TrainerProgramUpsertRequest request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        if (!user.IsTrainer)
        {
            throw new InvalidOperationException("Only trainer can manage workout programs.");
        }

        var trainer = await dbContext.Trainers.FirstOrDefaultAsync(x => x.UserId == user.Id, cancellationToken)
            ?? throw new InvalidOperationException("Trainer profile not found.");

        WorkoutProgram program;
        if (request.ProgramId.HasValue)
        {
            program = await dbContext.WorkoutPrograms.FirstOrDefaultAsync(
                x => x.Id == request.ProgramId.Value && x.TrainerId == trainer.Id,
                cancellationToken) ?? throw new InvalidOperationException("Program not found.");
        }
        else
        {
            program = new WorkoutProgram
            {
                TrainerId = trainer.Id,
                CreatedAtUtc = DateTime.UtcNow
            };

            dbContext.WorkoutPrograms.Add(program);
        }

        program.Title = request.Title;
        program.Description = request.Description;
        program.Difficulty = request.Difficulty;
        program.DurationMinutes = request.DurationMinutes;
        program.ProOnly = request.ProOnly;
        program.TrainerName = $"{trainer.FirstName} {trainer.LastName}".Trim();
        program.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return program;
    }

    public async Task<PaymentRecord> CreatePurchaseIntentAsync(PurchaseRequest request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");
        var plan = await dbContext.MembershipPlans.FirstOrDefaultAsync(x => x.Code == request.ProductCode, cancellationToken)
            ?? throw new InvalidOperationException("Unknown product code.");

        var payment = new PaymentRecord
        {
            UserId = user.Id,
            ProductCode = request.ProductCode,
            ProductType = request.ProductType,
            Amount = plan.Price,
            Currency = "RUB",
            Status = "awaiting_payment",
            MembershipPlanId = request.ProductType == "membership" ? plan.Id : null
        };

        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return payment;
    }

    public Task<PaymentRecord?> GetPaymentIntentAsync(Guid paymentId, CancellationToken cancellationToken)
        => dbContext.Payments.FirstOrDefaultAsync(x => x.Id == paymentId, cancellationToken);

    public async Task<PaymentRecord> ConfirmTelegramPaymentAsync(TelegramPaymentConfirmationRequest request, CancellationToken cancellationToken)
    {
        var payment = request.PaymentId.HasValue
            ? await dbContext.Payments.FirstOrDefaultAsync(x => x.Id == request.PaymentId.Value, cancellationToken)
            : await dbContext.Payments
                .Where(x => x.UserId == request.UserId && x.ProductCode == request.ProductCode && x.ProductType == request.ProductType)
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

        payment ??= new PaymentRecord
        {
            UserId = request.UserId,
            ProductCode = request.ProductCode,
            ProductType = request.ProductType
        };

        if (payment.Id == Guid.Empty)
        {
            dbContext.Payments.Add(payment);
        }
        else if (dbContext.Entry(payment).State == EntityState.Detached)
        {
            dbContext.Payments.Add(payment);
        }

        payment.Amount = request.Amount;
        payment.Currency = request.Currency;
        payment.Status = "paid";
        payment.Source = "telegram-bot";

        if (request.ProductType == "pro")
        {
            var active = await dbContext.ProSubscriptions
                .OrderByDescending(x => x.EndsAtUtc)
                .FirstOrDefaultAsync(x => x.UserId == request.UserId, cancellationToken);

            if (active is null)
            {
                active = new ProSubscription
                {
                    UserId = request.UserId,
                    StartsAtUtc = DateTime.UtcNow,
                    EndsAtUtc = DateTime.UtcNow.AddMonths(1),
                    Status = "active"
                };
                dbContext.ProSubscriptions.Add(active);
            }
            else
            {
                active.EndsAtUtc = active.EndsAtUtc < DateTime.UtcNow
                    ? DateTime.UtcNow.AddMonths(1)
                    : active.EndsAtUtc.AddMonths(1);
                active.Status = "active";
            }

            var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);
            if (user is not null)
            {
                user.IsProActive = true;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return payment;
    }

    private async Task PromoteTrainerIfNeededAsync(UserProfile user, CancellationToken cancellationToken)
    {
        if (!string.Equals(user.PhoneNumber, DefaultSeedData.PrimaryTrainerPhone, StringComparison.Ordinal))
        {
            return;
        }

        var trainer = await dbContext.Trainers.FirstOrDefaultAsync(
            x => x.PhoneNumber == user.PhoneNumber || x.UserId == user.Id,
            cancellationToken);

        if (trainer is null)
        {
            trainer = new TrainerProfile
            {
                UserId = user.Id,
                PhoneNumber = user.PhoneNumber,
                TelegramChatId = user.TelegramChatId,
                FirstName = string.IsNullOrWhiteSpace(user.FirstName) ? "Trainer" : user.FirstName,
                LastName = string.IsNullOrWhiteSpace(user.LastName) ? "PowerFitness" : user.LastName,
                Bio = "Main trainer account.",
                Specialization = "Strength training",
                AvatarUrl = user.AvatarUrl,
                CanManagePrograms = true
            };
            dbContext.Trainers.Add(trainer);
        }
        else
        {
            trainer.UserId = user.Id;
            trainer.PhoneNumber = user.PhoneNumber;
            trainer.TelegramChatId = user.TelegramChatId;
            trainer.FirstName = string.IsNullOrWhiteSpace(user.FirstName) ? trainer.FirstName : user.FirstName;
            trainer.LastName = string.IsNullOrWhiteSpace(user.LastName) ? trainer.LastName : user.LastName;
            trainer.AvatarUrl = string.IsNullOrWhiteSpace(user.AvatarUrl) ? trainer.AvatarUrl : user.AvatarUrl;
            trainer.CanManagePrograms = true;
        }

        user.IsTrainer = true;
        user.TrainerProfileId = trainer.Id;
    }
}
