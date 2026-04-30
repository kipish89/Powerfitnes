using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerFitness.Api.Data;
using PowerFitness.Api.Models;

namespace PowerFitness.Api.Controllers;

[ApiController]
[Route("api/trainers")]
public sealed class TrainersController(PowerFitnessDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => Ok(await dbContext.Trainers.AsNoTracking().OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ToListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.Trainers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TrainerProfile item, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(cancellationToken))
        {
            return Forbid();
        }

        dbContext.Trainers.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TrainerProfile request, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(cancellationToken))
        {
            return Forbid();
        }

        var item = await dbContext.Trainers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        item.UserId = request.UserId;
        item.PhoneNumber = request.PhoneNumber;
        item.TelegramChatId = request.TelegramChatId;
        item.FirstName = request.FirstName;
        item.LastName = request.LastName;
        item.Bio = request.Bio;
        item.Specialization = request.Specialization;
        item.AvatarUrl = request.AvatarUrl;
        item.CanManagePrograms = request.CanManagePrograms;
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

        var item = await dbContext.Trainers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        dbContext.Trainers.Remove(item);
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
