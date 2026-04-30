namespace PowerFitness.Api.Models;

public sealed class WorkoutProgram
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid? TrainerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public bool ProOnly { get; set; }
    public string TrainerName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
