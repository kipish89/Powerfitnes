using Microsoft.AspNetCore.Mvc;
using PowerFitness.Api.Services;

namespace PowerFitness.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(IFitnessRepository repository) : ControllerBase
{
    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> Get(Guid userId, CancellationToken cancellationToken)
        => Ok(await repository.GetDashboardAsync(userId, cancellationToken));
}
