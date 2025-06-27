using UpdateVersionManager.Models;

namespace UpdateVersionManager.Services;

public interface IFileService
{
    Task<string> CalculateFileHashAsync(string filePath);
    Task<bool> VerifyFileHashAsync(string filePath, string expectedHash);
    void CopyDirectory(string sourceDir, string targetDir);
    Task<VersionInfo> GenerateVersionInfoAsync(string version, string zipFilePath, string googleDriveFileId);
    Task SaveVersionInfoToJsonAsync(VersionInfo versionInfo, string filePath = "version_item.json");
}
