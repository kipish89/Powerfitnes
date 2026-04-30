using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerFitness.Api.Data;
using PowerFitness.Api.Models;
using PowerFitness.Api.Services;

namespace PowerFitness.Api.Controllers;

[ApiController]
[Route("api/files")]
public sealed class FilesController(
    PowerFitnessDbContext dbContext,
    IFileStorageService fileStorageService) : ControllerBase
{
    private static readonly string[] AllowedAvatarTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    private const long MaxAvatarSizeBytes = 5 * 1024 * 1024;

    [HttpPost("avatar/{userId:guid}")]
    [RequestSizeLimit(MaxAvatarSizeBytes)]
    public async Task<IActionResult> UploadAvatar(
        Guid userId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "File is required." });
        }

        if (file.Length > MaxAvatarSizeBytes)
        {
            return BadRequest(new { message = "File size must be less than 5 MB." });
        }

        if (!AllowedAvatarTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Only JPEG, PNG or WEBP images are allowed." });
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        await using var stream = file.OpenReadStream();
        var stored = await fileStorageService.SaveAvatarAsync(stream, file.FileName, cancellationToken);
        user.AvatarUrl = stored.RelativePath;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new FileUploadResult
        {
            FileName = stored.FileName,
            ContentType = file.ContentType,
            Size = file.Length,
            DownloadUrl = stored.RelativePath
        });
    }

    [HttpGet("avatar/{fileName}")]
    public IActionResult DownloadAvatar(string fileName)
    {
        var physicalPath = fileStorageService.GetAvatarPhysicalPath(fileName);
        if (!System.IO.File.Exists(physicalPath))
        {
            return NotFound();
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var contentType = extension switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };

        return PhysicalFile(physicalPath, contentType);
    }
}
