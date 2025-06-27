namespace UpdateVersionManager.Services;

public interface IUniversalDownloadService : IDisposable
{
    Task<string> DownloadTextAsync(string url);
    Task DownloadFileAsync(string url, string filePath);
}
