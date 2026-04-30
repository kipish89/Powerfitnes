using PowerFitness.Api.Models;

namespace PowerFitness.Api.Services;

public interface IFitnessRepository
{
    Task<TelegramRegistrationTicket> StartPhoneRegistrationAsync(string phoneNumber, CancellationToken cancellationToken);
    Task<TelegramRegistrationTicket?> GetTicketAsync(Guid ticketId, CancellationToken cancellationToken);
    Task<UserProfile> ConfirmTelegramRegistrationAsync(TelegramConfirmationRequest request, CancellationToken cancellationToken);
    Task<UserProfile?> FindUserByPhoneAsync(string phoneNumber, CancellationToken cancellationToken);
    Task<DashboardResponse> GetDashboardAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<MembershipPlan>> GetMembershipPlansAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkoutProgram>> GetWorkoutProgramsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<TrainerProfile>> GetTrainersAsync(CancellationToken cancellationToken);
    Task<TrainerProfile?> GetTrainerByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<WorkoutProgram> SaveTrainerProgramAsync(TrainerProgramUpsertRequest request, CancellationToken cancellationToken);
    Task<PaymentRecord> CreatePurchaseIntentAsync(PurchaseRequest request, CancellationToken cancellationToken);
    Task<PaymentRecord?> GetPaymentIntentAsync(Guid paymentId, CancellationToken cancellationToken);
    Task<PaymentRecord> ConfirmTelegramPaymentAsync(TelegramPaymentConfirmationRequest request, CancellationToken cancellationToken);
}
