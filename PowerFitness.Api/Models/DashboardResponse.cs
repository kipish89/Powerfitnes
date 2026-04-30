namespace PowerFitness.Api.Models;

public sealed class DashboardResponse
{
    public UserProfile? User { get; set; }
    public TrainerProfile? TrainerProfile { get; set; }
    public IReadOnlyList<MembershipPlan> MembershipPlans { get; set; } = [];
    public IReadOnlyList<WorkoutProgram> WorkoutPrograms { get; set; } = [];
    public IReadOnlyList<TrainerProfile> Trainers { get; set; } = [];
    public IReadOnlyList<PaymentRecord> Payments { get; set; } = [];
    public ProSubscription? ProSubscription { get; set; }
}
