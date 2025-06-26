using System.IO.Compression;
using System.Text.Json;
using UpdateVersionManager.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace UpdateVersionManager.Services;

public class VersionManager
{
    private readonly GoogleDriveService _googleDriveService;
    private readonly FileService _fileService;
    private readonly SymbolicLinkService _symbolicLinkService;
    private readonly UpdateVersionManagerSettings _settings;
    private readonly ILogger<VersionManager> _logger;

    public VersionManager(
        GoogleDriveService googleDriveService,
        FileService fileService,
        SymbolicLinkService symbolicLinkService,
        IOptions<UpdateVersionManagerSettings> settings,
        ILogger<VersionManager> logger)
    {
        _googleDriveService = googleDriveService;
        _fileService = fileService;
        _symbolicLinkService = symbolicLinkService;
        _settings = settings.Value;
        _logger = logger;
    }

    public string? GetCurrentVersion()
    {
        if (File.Exists(_settings.CurrentVersionFile))
            return File.ReadAllText(_settings.CurrentVersionFile).Trim();
        return null;
    }

    public List<string> GetInstalledVersions()
    {
        if (!Directory.Exists(_settings.LocalBaseDir))
            return new List<string>();

        return Directory.GetDirectories(_settings.LocalBaseDir)
            .Select(d => Path.GetFileName(d))
            .OrderByDescending(v => v)
            .ToList();
    }

    public async Task<List<VersionInfo>> GetRemoteVersionsAsync()
    {
        try
        {
            var json = await _googleDriveService.DownloadTextAsync(_settings.VersionListUrl);
            var versionListData = JsonSerializer.Deserialize<JsonElement>(json);

            if (!versionListData.TryGetProperty("versions", out var versionsArray))
            {
                throw new JsonException("JSON 中找不到 'versions' 屬性");
            }

            var versionList = new List<VersionInfo>();

            foreach (var versionElement in versionsArray.EnumerateArray())
            {
                var version = GetStringProperty(versionElement, "version", "Version");
                var downloadUrl = GetStringProperty(versionElement, "downloadUrl", "DownloadUrl");
                var sha256 = GetStringProperty(versionElement, "sha256", "Sha256");
                var size = GetLongProperty(versionElement, "size", "Size");
                var releaseDate = GetStringProperty(versionElement, "releaseDate", "ReleaseDate");
                var description = GetStringProperty(versionElement, "description", "Description");

                if (!string.IsNullOrEmpty(version) && !string.IsNullOrEmpty(downloadUrl))
                {
                    versionList.Add(new VersionInfo
                    {
                        Version = version,
                        DownloadUrl = downloadUrl,
                        Sha256 = sha256 ?? string.Empty,
                        Size = size,
                        ReleaseDate = releaseDate ?? string.Empty,
                        Description = description ?? string.Empty
                    });
                }
            }

            return versionList;
        }
        catch (JsonException ex)
        {
            throw new Exception($"解析版本清單失敗: {ex.Message}");
        }
        catch (Exception ex)
        {
            throw new Exception($"取得版本清單失敗: {ex.Message}");
        }
    }

    // 輔助方法：支援兩種命名格式
    private string? GetStringProperty(JsonElement element, string camelCaseName, string pascalCaseName)
    {
        if (element.TryGetProperty(camelCaseName, out var camelProp))
            return camelProp.GetString();

        if (element.TryGetProperty(pascalCaseName, out var pascalProp))
            return pascalProp.GetString();

        return null;
    }

    private long GetLongProperty(JsonElement element, string camelCaseName, string pascalCaseName)
    {
        if (element.TryGetProperty(camelCaseName, out var camelProp))
            return camelProp.GetInt64();

        if (element.TryGetProperty(pascalCaseName, out var pascalProp))
            return pascalProp.GetInt64();

        return 0;
    }

    public async Task UseVersionAsync(string version)
    {
        _logger.LogInformation("嘗試切換到版本 {Version}", version);
        var versionDir = Path.Combine(_settings.LocalBaseDir, version);
        if (!Directory.Exists(versionDir))
        {
            _logger.LogWarning("版本 {Version} 不存在", version);
            Console.WriteLine($"版本 {version} 不存在");
            Console.WriteLine("使用 'uvm list' 查看可用版本");
            return;
        }

        await SetCurrentVersionAsync(version);
        _logger.LogInformation("成功切換至版本 {Version}", version);
        Console.WriteLine($"已切換至版本 {version}");
    }

    public Task CleanVersionAsync(string version)
    {
        var versionDir = Path.Combine(_settings.LocalBaseDir, version);
        if (!Directory.Exists(versionDir))
        {
            _logger.LogWarning("嘗試刪除不存在的版本 {Version}", version);
            Console.WriteLine($"版本 {version} 不存在");
            return Task.CompletedTask;
        }

        var currentVersion = GetCurrentVersion();
        if (version == currentVersion)
        {
            _logger.LogWarning("嘗試刪除當前使用的版本 {Version}", version);
            Console.WriteLine($"無法刪除當前使用的版本 {version}");
            return Task.CompletedTask;
        }

        Directory.Delete(versionDir, true);
        _logger.LogInformation("已刪除版本 {Version}", version);
        Console.WriteLine($"已刪除版本 {version}");
        return Task.CompletedTask;
    }

    public async Task AutoUpdateAsync()
    {
        try
        {
            _logger.LogInformation("開始自動更新檢查");
            Console.WriteLine("[Updater] 檢查更新中...");
            var versionList = await GetRemoteVersionsAsync();

            if (!versionList.Any())
            {
                _logger.LogWarning("無可用版本");
                Console.WriteLine("[Updater] 無可用版本");
                return;
            }

            var latestVersion = versionList.OrderByDescending(v => v.Version).First();
            var currentVersion = GetCurrentVersion();

            _logger.LogInformation("版本比較 - 最新: {LatestVersion}, 當前: {CurrentVersion}", 
                latestVersion.Version, currentVersion);
            Console.WriteLine($"[Updater] 最新版本: {latestVersion.Version}");
            Console.WriteLine($"[Updater] 當前版本: {currentVersion}");

            if (latestVersion.Version == currentVersion)
            {
                _logger.LogInformation("已是最新版本，無需更新");
                Console.WriteLine("[Updater] 已是最新版本");
                return;
            }

            _logger.LogInformation("發現新版本 {NewVersion}，開始自動更新", latestVersion.Version);
            Console.WriteLine($"[Updater] 發現新版本 {latestVersion.Version}，開始自動更新...");
            await InstallVersionSilentAsync(latestVersion.Version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "自動更新過程發生錯誤");
            Console.WriteLine($"[Updater] 更新失敗: {ex.Message}");
        }
    }

    // 新增一個靜默安裝方法，專門給自動更新使用
    private async Task InstallVersionSilentAsync(string version)
    {
        _logger.LogInformation("開始靜默安裝版本 {Version}", version);
        var versionDir = Path.Combine(_settings.LocalBaseDir, version);

        // 檢查版本是否已安裝
        if (Directory.Exists(versionDir))
        {
            _logger.LogInformation("版本 {Version} 已存在，直接切換", version);
            Console.WriteLine($"[Updater] 版本 {version} 已存在，直接切換");
            await SetCurrentVersionAsync(version);
            Console.WriteLine($"[Updater] ✅ 已切換到版本 {version}");
            return;
        }

        // 取得版本資訊
        var versionList = await GetRemoteVersionsAsync();
        var versionInfo = versionList.FirstOrDefault(v => v.Version == version);

        if (versionInfo == null)
        {
            _logger.LogError("找不到版本資訊 {Version}", version);
            Console.WriteLine($"[Updater] ❌ 找不到版本 {version}");
            return;
        }

        _logger.LogInformation("開始下載版本 {Version}，下載URL: {DownloadUrl}", version, versionInfo.DownloadUrl);
        Console.WriteLine($"[Updater] 正在下載版本 {version}...");

        // 下載檔案
        await _googleDriveService.DownloadFileAsync(versionInfo.DownloadUrl, _settings.ZipFilePath);

        // 驗證 SHA256
        if (!string.IsNullOrEmpty(versionInfo.Sha256))
        {
            _logger.LogInformation("開始驗證檔案完整性，期待 SHA256: {ExpectedHash}", versionInfo.Sha256);
            Console.WriteLine("[Updater] 驗證檔案完整性...");
            if (!await _fileService.VerifyFileHashAsync(_settings.ZipFilePath, versionInfo.Sha256))
            {
                _logger.LogError("檔案 SHA256 驗證失敗，期待: {ExpectedHash}", versionInfo.Sha256);
                Console.WriteLine("[Updater] ❌ 檔案驗證失敗，安裝中止");
                return;
            }
            _logger.LogInformation("檔案完整性驗證通過");
        }

        // 解壓縮
        if (Directory.Exists(_settings.TempExtractPath))
            Directory.Delete(_settings.TempExtractPath, true);

        _logger.LogInformation("開始解壓縮到臨時目錄: {TempPath}", _settings.TempExtractPath);
        Console.WriteLine("[Updater] 解壓縮中...");
        ZipFile.ExtractToDirectory(_settings.ZipFilePath, _settings.TempExtractPath);

        // 移動到版本目錄
        Directory.CreateDirectory(_settings.LocalBaseDir);
        Directory.Move(_settings.TempExtractPath, versionDir);

        _logger.LogInformation("版本 {Version} 安裝完成，安裝路徑: {InstallPath}", version, versionDir);
        Console.WriteLine($"[Updater] 版本 {version} 安裝完成");

        // 自動切換到新版本（無需確認）
        await SetCurrentVersionAsync(version);
        _logger.LogInformation("已自動切換到版本 {Version}", version);
        Console.WriteLine($"[Updater] ✅ 已自動切換到版本 {version}");

        // 清理
        if (File.Exists(_settings.ZipFilePath))
        {
            File.Delete(_settings.ZipFilePath);
            _logger.LogDebug("清理下載檔案 {ZipFile}", _settings.ZipFilePath);
        }
    }

    // 修改原有的 InstallVersionAsync 方法，保持互動式安裝的行為
    public async Task InstallVersionAsync(string version)
    {
        var versionDir = Path.Combine(_settings.LocalBaseDir, version);

        // 檢查版本是否已安裝
        if (Directory.Exists(versionDir))
        {
            Console.WriteLine($"版本 {version} 已安裝");
            Console.Write("是否要重新安裝? (y/N): ");
            var response = Console.ReadLine();
            if (response?.ToLower() != "y")
                return;

            Directory.Delete(versionDir, true);
            Console.WriteLine($"已移除舊版本 {version}");
        }

        // 取得版本資訊
        var versionList = await GetRemoteVersionsAsync();
        var versionInfo = versionList.FirstOrDefault(v => v.Version == version);

        if (versionInfo == null)
        {
            Console.WriteLine($"找不到版本 {version}");
            Console.WriteLine("使用 'uvm ls-remote' 查看可用版本");
            return;
        }

        Console.WriteLine($"正在安裝版本 {version}...");

        // 下載檔案
        Console.WriteLine("下載中...");
        await _googleDriveService.DownloadFileAsync(versionInfo.DownloadUrl, _settings.ZipFilePath);

        // 驗證 SHA256
        if (!string.IsNullOrEmpty(versionInfo.Sha256))
        {
            Console.WriteLine("驗證檔案完整性...");
            if (!await _fileService.VerifyFileHashAsync(_settings.ZipFilePath, versionInfo.Sha256))
            {
                Console.WriteLine("檔案驗證失敗，安裝中止");
                return;
            }
        }

        // 解壓縮
        if (Directory.Exists(_settings.TempExtractPath))
            Directory.Delete(_settings.TempExtractPath, true);

        Console.WriteLine("解壓縮中...");
        ZipFile.ExtractToDirectory(_settings.ZipFilePath, _settings.TempExtractPath);

        // 移動到版本目錄
        Directory.CreateDirectory(_settings.LocalBaseDir);
        Directory.Move(_settings.TempExtractPath, versionDir);

        Console.WriteLine($"✅ 版本 {version} 安裝完成");

        // 詢問是否要切換到新版本（互動式）
        Console.Write($"是否要切換到版本 {version}? (Y/n): ");
        var switchResponse = Console.ReadLine();
        if (switchResponse?.ToLower() != "n")
        {
            await SetCurrentVersionAsync(version);
            Console.WriteLine($"已切換到版本 {version}");
        }

        // 清理
        if (File.Exists(_settings.ZipFilePath))
            File.Delete(_settings.ZipFilePath);
    }

    private async Task SetCurrentVersionAsync(string version)
    {
        // 更新版本記錄
        await File.WriteAllTextAsync(_settings.CurrentVersionFile, version);

        // 更新捷徑資料夾
        var versionDir = Path.Combine(_settings.LocalBaseDir, version);
        var linkPath = Path.Combine(Directory.GetCurrentDirectory(), _settings.AppLinkName);

        await _symbolicLinkService.UpdateAppLinkAsync(version, versionDir, linkPath);
    }
}