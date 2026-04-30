namespace PowerFitness.Api.Models;

public sealed class TrainerProgramUpsertRequest
{
    public Guid UserId { get; set; }
    public Guid? ProgramId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public bool ProOnly { get; set; }
}
