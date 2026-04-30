using Microsoft.AspNetCore.Mvc;
using PowerFitness.Api.Models;
using PowerFitness.Api.Services;

namespace PowerFitness.Api.Controllers;

[ApiController]
[Route("api/trainer/programs")]
public sealed class TrainerProgramsController(IFitnessRepository repository) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] TrainerProgramUpsertRequest request, CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
        {
            return BadRequest(new { message = "User is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { message = "Program title is required." });
        }

        if (request.DurationMinutes <= 0)
        {
            return BadRequest(new { message = "Duration must be greater than zero." });
        }

        try
        {
            var program = await repository.SaveTrainerProgramAsync(request, cancellationToken);
            return Ok(program);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
