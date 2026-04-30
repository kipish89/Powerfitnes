using Microsoft.EntityFrameworkCore;
using PowerFitness.Api.Models;

namespace PowerFitness.Api.Data;

public sealed class PowerFitnessDbContext(DbContextOptions<PowerFitnessDbContext> options) : DbContext(options)
{
    public DbSet<UserProfile> Users => Set<UserProfile>();
    public DbSet<TelegramRegistrationTicket> RegistrationTickets => Set<TelegramRegistrationTicket>();
    public DbSet<MembershipPlan> MembershipPlans => Set<MembershipPlan>();
    public DbSet<WorkoutProgram> WorkoutPrograms => Set<WorkoutProgram>();
    public DbSet<TrainerProfile> Trainers => Set<TrainerProfile>();
    public DbSet<PaymentRecord> Payments => Set<PaymentRecord>();
    public DbSet<ProSubscription> ProSubscriptions => Set<ProSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TelegramRegistrationTicket>()
            .HasKey(x => x.TicketId);

        modelBuilder.Entity<UserProfile>()
            .HasIndex(x => x.PhoneNumber)
            .IsUnique();

        modelBuilder.Entity<MembershipPlan>()
            .HasIndex(x => x.Code)
            .IsUnique();

        modelBuilder.Entity<TrainerProfile>()
            .HasIndex(x => x.PhoneNumber);

        modelBuilder.Entity<PaymentRecord>()
            .HasOne(x => x.User)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProSubscription>()
            .HasOne(x => x.User)
            .WithMany(x => x.ProSubscriptions)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        base.OnModelCreating(modelBuilder);
    }
}
