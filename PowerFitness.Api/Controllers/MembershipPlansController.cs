using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerFitness.Api.Data;
using PowerFitness.Api.Models;

namespace PowerFitness.Api.Controllers;

[ApiController]
[Route("api/memberships")]
public sealed class MembershipPlansController(PowerFitnessDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        => Ok(await dbContext.MembershipPlans.AsNoTracking().OrderBy(x => x.DurationMonths).ToListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.MembershipPlans.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MembershipPlan item, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(cancellationToken))
        {
            return Forbid();
        }

        dbContext.MembershipPlans.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] MembershipPlan request, CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(cancellationToken))
        {
            return Forbid();
        }

        var item = await dbContext.MembershipPlans.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        item.Code = request.Code;
        item.Title = request.Title;
        item.Description = request.Description;
        item.DurationMonths = request.DurationMonths;
        item.Price = request.Price;
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

        var item = await dbContext.MembershipPlans.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        dbContext.MembershipPlans.Remove(item);
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
