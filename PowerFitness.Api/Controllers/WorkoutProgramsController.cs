using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerFitness.Api.Data;
using PowerFitness.Api.Models;

namespace PowerFitness.Api.Controllers;

[ApiController]
[Route("api/workouts")]
public sealed class WorkoutProgramsController(PowerFitnessDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => Ok(await dbContext.WorkoutPrograms.AsNoTracking().OrderByDescending(x => x.UpdatedAtUtc).ToListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.WorkoutPrograms.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WorkoutProgram item, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(cancellationToken))
        {
            return Forbid();
        }

        item.UpdatedAtUtc = DateTime.UtcNow;
        if (item.CreatedAtUtc == default)
        {
            item.CreatedAtUtc = DateTime.UtcNow;
        }

        dbContext.WorkoutPrograms.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] WorkoutProgram request, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(cancellationToken))
        {
            return Forbid();
        }

        var item = await dbContext.WorkoutPrograms.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        item.Title = request.Title;
        item.Description = request.Description;
        item.Difficulty = request.Difficulty;
        item.DurationMinutes = request.DurationMinutes;
        item.ProOnly = request.ProOnly;
        item.TrainerName = request.TrainerName;
        item.TrainerId = request.TrainerId;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(item);
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(cancellationToken))
        {
            return Forbid();
        }

        var item = await dbContext.WorkoutPrograms.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        dbContext.WorkoutPrograms.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<bool> CanManageAsync(CancellationToken cancellationToken)
    {
        var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(rawUserId, out var userId))
        {
            return false;
        }

        return await dbContext.Users.AnyAsync(x => x.Id == userId && x.IsTrainer, cancellationToken);
    }
}
