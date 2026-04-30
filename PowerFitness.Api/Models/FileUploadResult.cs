namespace PowerFitness.Api.Models;

public sealed class FileUploadResult
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
}
