namespace UpdateVersionManager.Services;

public interface IGoogleDriveService : IDisposable
{
    Task<string> DownloadTextAsync(string url);
    Task DownloadFileAsync(string url, string filePath);
}
