namespace UpdateVersionManager.Services;

public interface ISymbolicLinkService
{
    Task<bool> CreateSymbolicLinkAsync(string linkPath, string targetPath);
    Task<bool> RemoveSymbolicLinkAsync(string linkPath);
    bool IsSymbolicLink(string path);
    string? GetSymbolicLinkTarget(string linkPath);
    Task UpdateAppLinkAsync(string version, string versionDir, string linkPath);
    void ShowLinkInfo(string linkPath);
}
