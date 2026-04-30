using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerFitness.Api.Data;
using PowerFitness.Api.Models;

namespace PowerFitness.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController(PowerFitnessDbContext dbContext) : ControllerBase
{
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        if (!await IsTrainerAsync(cancellationToken))
        {
            return Forbid();
        }

        return Ok(await dbContext.Users.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken));
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!await CanAccessUserAsync(id, cancellationToken))
        {
            return Forbid();
        }

        var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UserProfile request, CancellationToken cancellationToken)
    {
        if (!await CanAccessUserAsync(id, cancellationToken))
        {
            return Forbid();
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        user.PhoneNumber = request.PhoneNumber;
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Gender = request.Gender;
        user.AvatarUrl = request.AvatarUrl;
        user.TelegramConfirmed = request.TelegramConfirmed;
        user.TelegramChatId = request.TelegramChatId;
        user.IsProActive = request.IsProActive;
        user.IsTrainer = request.IsTrainer;
        user.TrainerProfileId = request.TrainerProfileId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(user);
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!await CanAccessUserAsync(id, cancellationToken))
        {
            return Forbid();
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<bool> IsTrainerAsync(CancellationToken cancellationToken)
    {
        var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(rawUserId, out var userId))
        {
            return false;
        }

        return await dbContext.Users.AnyAsync(x => x.Id == userId && x.IsTrainer, cancellationToken);
    }

    private async Task<bool> CanAccessUserAsync(Guid targetUserId, CancellationToken cancellationToken)
    {
        var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(rawUserId, out var currentUserId))
        {
            return false;
        }

        if (currentUserId == targetUserId)
        {
            return true;
        }

        return await dbContext.Users.AnyAsync(x => x.Id == currentUserId && x.IsTrainer, cancellationToken);
    }
}
