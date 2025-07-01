using System.Security.Cryptography;
using System.Text.Json;
using UpdateVersionManager.Models;
using Microsoft.Extensions.Logging;

namespace UpdateVersionManager.Services;

public class FileService : IFileService
{
    private readonly ILogger<FileService> _logger;

    public FileService(ILogger<FileService> logger)
    {
        _logger = logger;
    }

    public async Task<string> CalculateFileHashAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogError("檔案不存在: {FilePath}", filePath);
            throw new FileNotFoundException($"檔案不存在: {filePath}");
        }

        var fileInfo = new FileInfo(filePath);
        _logger.LogInformation("開始計算檔案 SHA256，檔案: {FilePath}，大小: {Size} bytes", filePath, fileInfo.Length);

        using var sha256 = SHA256.Create();
        await using var fileStream = File.OpenRead(filePath);
        var hashBytes = await sha256.ComputeHashAsync(fileStream);
        var hash = BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLowerInvariant();
        
        _logger.LogInformation("檔案 SHA256 計算完成: {Hash}", hash);
        return hash;
    }

    public async Task<bool> VerifyFileHashAsync(string filePath, string expectedHash)
    {
        _logger.LogInformation("開始驗證檔案 SHA256，檔案: {FilePath}", filePath);
        var actualHash = await CalculateFileHashAsync(filePath);
        var isValid = actualHash == expectedHash.ToLowerInvariant();
        
        _logger.LogInformation("SHA256 驗證結果: {Result}，期待: {Expected}，實際: {Actual}", 
            isValid ? "通過" : "失敗", expectedHash.ToLowerInvariant(), actualHash);
            
        return isValid;
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

    public async Task<VersionInfo> GenerateVersionInfoAsync(string version, string zipFilePath, string downloadUrl)
    {
        if (!File.Exists(zipFilePath))
            throw new FileNotFoundException($"ZIP 檔案不存在: {zipFilePath}");

        var sha256 = await CalculateFileHashAsync(zipFilePath);
        var fileSize = new FileInfo(zipFilePath).Length;

        return new VersionInfo
        {
            Version = version,
            DownloadUrl = downloadUrl,
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