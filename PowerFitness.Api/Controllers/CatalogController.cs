using Microsoft.AspNetCore.Mvc;
using PowerFitness.Api.Services;

namespace PowerFitness.Api.Controllers;

[ApiController]
[Route("api/catalog")]
public sealed class CatalogController(IFitnessRepository repository) : ControllerBase
{
    [HttpGet("memberships")]
    public async Task<IActionResult> GetMemberships(CancellationToken cancellationToken)
        => Ok(await repository.GetMembershipPlansAsync(cancellationToken));

    [HttpGet("workouts")]
    public async Task<IActionResult> GetWorkouts(CancellationToken cancellationToken)
        => Ok(await repository.GetWorkoutProgramsAsync(cancellationToken));

    [HttpGet("trainers")]
    public async Task<IActionResult> GetTrainers(CancellationToken cancellationToken)
        => Ok(await repository.GetTrainersAsync(cancellationToken));
}
