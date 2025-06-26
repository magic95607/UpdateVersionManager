using System.Security.Cryptography;
using System.Text.Json;
using UpdateVersionManager.Models;

namespace UpdateVersionManager.Services;

public class FileService
{
    public async Task<string> CalculateFileHashAsync(string filePath)
    {
        using var sha256 = SHA256.Create();
        await using var fileStream = File.OpenRead(filePath);
        var hashBytes = await sha256.ComputeHashAsync(fileStream);
        return BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLowerInvariant();
    }

    public async Task<bool> VerifyFileHashAsync(string filePath, string expectedHash)
    {
        var actualHash = await CalculateFileHashAsync(filePath);
        return actualHash == expectedHash.ToLowerInvariant();
    }

    public void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, file);
            var targetFile = Path.Combine(targetDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
            File.Copy(file, targetFile, true);
        }
    }

    public async Task<VersionInfo> GenerateVersionInfoAsync(string version, string zipFilePath, string googleDriveFileId)
    {
        if (!File.Exists(zipFilePath))
            throw new FileNotFoundException($"ZIP 檔案不存在: {zipFilePath}");

        var sha256 = await CalculateFileHashAsync(zipFilePath);
        var fileSize = new FileInfo(zipFilePath).Length;

        return new VersionInfo
        {
            Version = version,
            DownloadUrl = $"https://drive.google.com/uc?export=download&id={googleDriveFileId}",
            Sha256 = sha256,
            Size = fileSize,
            ReleaseDate = DateTime.Now.ToString("yyyy-MM-dd"),
            Description = $"Version {version}"
        };
    }

    public async Task SaveVersionInfoToJsonAsync(VersionInfo versionInfo, string filePath = "version_item.json")
    {
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // 使用 camelCase 命名
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var json = JsonSerializer.Serialize(versionInfo, jsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }
}