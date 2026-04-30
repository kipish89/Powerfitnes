namespace PowerFitness.Api.Services;

public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _avatarDirectory = Path.Combine(AppContext.BaseDirectory, "uploads", "avatars");

    public async Task<(string FileName, string RelativePath)> SaveAvatarAsync(
        Stream stream,
        string originalFileName,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_avatarDirectory);
        var extension = Path.GetExtension(originalFileName);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(_avatarDirectory, fileName);

        await using var fileStream = File.Create(physicalPath);
        await stream.CopyToAsync(fileStream, cancellationToken);

        return (fileName, $"api/files/avatar/{fileName}");
    }

    public string GetAvatarPhysicalPath(string fileName)
        => Path.Combine(_avatarDirectory, fileName);
}
