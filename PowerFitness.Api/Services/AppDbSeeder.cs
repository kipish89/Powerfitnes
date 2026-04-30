using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PowerFitness.Api.Data;
using PowerFitness.Api.Models;

namespace PowerFitness.Api.Services;

public static class AppDbSeeder
{
    public static async Task SeedAsync(PowerFitnessDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        if (!await dbContext.MembershipPlans.AnyAsync(cancellationToken))
        {
            dbContext.MembershipPlans.AddRange(DefaultSeedData.MembershipPlans);
        }

        if (!await dbContext.Trainers.AnyAsync(cancellationToken))
        {
            dbContext.Trainers.AddRange(DefaultSeedData.Trainers);
        }

        if (!await dbContext.WorkoutPrograms.AnyAsync(cancellationToken))
        {
            dbContext.WorkoutPrograms.AddRange(DefaultSeedData.WorkoutPrograms);
        }

        if (!await dbContext.Users.AnyAsync(x => x.Id == DefaultSeedData.DemoUser.Id, cancellationToken))
        {
            var passwordHasher = new PasswordHasher<UserProfile>();
            var demoUser = DefaultSeedData.DemoUser;
            demoUser.PasswordHash = passwordHasher.HashPassword(demoUser, "demo123");
            dbContext.Users.Add(demoUser);
            dbContext.Payments.Add(DefaultSeedData.DemoPayment);
            dbContext.ProSubscriptions.Add(DefaultSeedData.DemoProSubscription);
        }

        if (!await dbContext.Users.AnyAsync(x => x.Id == DefaultSeedData.PrimaryTrainerUser.Id, cancellationToken))
        {
            var passwordHasher = new PasswordHasher<UserProfile>();
            var trainerUser = DefaultSeedData.PrimaryTrainerUser;
            trainerUser.PasswordHash = passwordHasher.HashPassword(trainerUser, "trainer123");
            dbContext.Users.Add(trainerUser);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
