namespace PowerFitness.Api.Services;

public interface IFileStorageService
{
    Task<(string FileName, string RelativePath)> SaveAvatarAsync(Stream stream, string originalFileName, CancellationToken cancellationToken);
    string GetAvatarPhysicalPath(string fileName);
}
