using System.IO.Compression;
using System.Text.Json;
using UpdateVersionManager.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace UpdateVersionManager.Services;

public class VersionManager
{
    private readonly IUniversalDownloadService _downloadService;
    private readonly IFileService _fileService;
    private readonly ISymbolicLinkService _symbolicLinkService;
    private readonly UpdateVersionManagerSettings _settings;
    private readonly ILogger<VersionManager> _logger;
    private readonly ConfigPathProvider? _configPathProvider;

    public VersionManager(
        IUniversalDownloadService downloadService,
        IFileService fileService,
        ISymbolicLinkService symbolicLinkService,
        IOptions<UpdateVersionManagerSettings> settings,
        ILogger<VersionManager> logger,
        ConfigPathProvider? configPathProvider = null)
    {
        _downloadService = downloadService;
        _fileService = fileService;
        _symbolicLinkService = symbolicLinkService;
        _settings = settings.Value;
        _logger = logger;
        _configPathProvider = configPathProvider;
    }

    public virtual string? GetCurrentVersion()
    {
        if (File.Exists(_settings.CurrentVersionFile))
            return File.ReadAllText(_settings.CurrentVersionFile).Trim();
        return null;
    }

    public virtual List<string> GetInstalledVersions()
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
            string json;
            var versionListSource = _settings.GetVersionListSource();
            
            // 判斷是本地檔案還是遠端 URL
            if (IsLocalPath(versionListSource))
            {
                // 處理相對路徑和絕對路徑
                var filePath = ResolveLocalPath(versionListSource);
                _logger.LogInformation("從本地檔案讀取版本清單: {Source}", filePath);
                
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"版本清單檔案不存在: {filePath}");
                }
                
                json = await File.ReadAllTextAsync(filePath);
            }
            else
            {
                _logger.LogInformation("從遠端下載版本清單: {Source}", versionListSource);
                json = await _downloadService.DownloadTextAsync(versionListSource);
            }
            
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

    private bool IsLocalPath(string path)
    {
        // 檢查是否為 URL
        if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
        {
            return uri.Scheme == "file" || string.IsNullOrEmpty(uri.Scheme);
        }
        
        // 檢查是否為相對路徑或絕對路徑
        return !path.Contains("://");
    }

    private string ResolveLocalPath(string path)
    {
        // 如果是絕對路徑，直接返回
        if (Path.IsPathRooted(path))
        {
            return path;
        }
        
        // 如果是相對路徑，先嘗試相對於設定檔所在目錄
        var configPath = GetCurrentConfigPath();
        if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
        {
            var configDirectory = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrEmpty(configDirectory))
            {
                var resolvedPath = Path.Combine(configDirectory, path);
                if (File.Exists(resolvedPath))
                {
                    _logger.LogDebug("解析相對路徑基於設定檔目錄: {Path} -> {ResolvedPath}", path, resolvedPath);
                    return resolvedPath;
                }
            }
        }
        
        // 如果設定檔相對路徑不存在，嘗試相對於當前工作目錄
        var currentDirPath = Path.Combine(Directory.GetCurrentDirectory(), path);
        if (File.Exists(currentDirPath))
        {
            _logger.LogDebug("解析相對路徑基於當前目錄: {Path} -> {ResolvedPath}", path, currentDirPath);
            return currentDirPath;
        }
        
        // 如果都不存在，返回原路徑（讓後續錯誤處理機制處理）
        return path;
    }

    private string? GetCurrentConfigPath()
    {
        return _configPathProvider?.ConfigPath;
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

    public virtual async Task UseVersionAsync(string version)
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

    public virtual Task CleanVersionAsync(string version)
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

    public virtual async Task AutoUpdateAsync()
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
        await _downloadService.DownloadFileAsync(versionInfo.DownloadUrl, _settings.ZipFilePath);

        // 驗證 SHA256
        if (!string.IsNullOrEmpty(versionInfo.Sha256))
        {
            _logger.LogInformation("開始驗證檔案完整性，期待 SHA256: {ExpectedHash}", versionInfo.Sha256);
            Console.WriteLine("[Updater] 驗證檔案完整性...");
            
            var actualHash = await _fileService.CalculateFileHashAsync(_settings.ZipFilePath);
            _logger.LogInformation("檔案實際 SHA256: {ActualHash}", actualHash);
            
            if (!await _fileService.VerifyFileHashAsync(_settings.ZipFilePath, versionInfo.Sha256))
            {
                _logger.LogError("檔案 SHA256 驗證失敗，期待: {ExpectedHash}，實際: {ActualHash}", versionInfo.Sha256, actualHash);
                Console.WriteLine("[Updater] ❌ 檔案驗證失敗，安裝中止");
                Console.WriteLine($"[Updater] 期待的 SHA256: {versionInfo.Sha256}");
                Console.WriteLine($"[Updater] 實際的 SHA256: {actualHash}");
                return;
            }
            _logger.LogInformation("檔案完整性驗證通過");
        }

        // 解壓縮
        if (Directory.Exists(_settings.TempExtractPath))
            Directory.Delete(_settings.TempExtractPath, true);

        _logger.LogInformation("開始解壓縮到臨時目錄: {TempPath}", _settings.TempExtractPath);
        Console.WriteLine("[Updater] 解壓縮中...");
        
        // 使用支援 UTF-8 編碼的解壓方式來處理中文檔案名稱
        using (var archive = ZipFile.OpenRead(_settings.ZipFilePath))
        {
            foreach (var entry in archive.Entries)
            {
                // 確保目標目錄存在
                var destinationPath = Path.Combine(_settings.TempExtractPath, entry.FullName);
                var destinationDir = Path.GetDirectoryName(destinationPath);
                
                if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }
                
                // 如果是檔案則解壓縮
                if (!string.IsNullOrEmpty(entry.Name))
                {
                    try
                    {
                        entry.ExtractToFile(destinationPath, overwrite: true);
                        _logger.LogDebug("解壓縮檔案: {FileName}", entry.FullName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "解壓縮檔案失敗: {FileName}", entry.FullName);
                        throw new Exception($"解壓縮檔案失敗: {entry.FullName} - {ex.Message}");
                    }
                }
            }
        }

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
    public virtual async Task InstallVersionAsync(string version)
    {
        _logger.LogInformation("開始互動式安裝版本 {Version}", version);
        var versionDir = Path.Combine(_settings.LocalBaseDir, version);

        // 檢查版本是否已安裝
        if (Directory.Exists(versionDir))
        {
            _logger.LogInformation("版本 {Version} 已存在，詢問是否重新安裝", version);
            Console.WriteLine($"版本 {version} 已安裝");
            Console.Write("是否要重新安裝? (y/N): ");
            var response = Console.ReadLine();
            if (response?.ToLower() != "y")
            {
                _logger.LogInformation("使用者取消重新安裝版本 {Version}", version);
                return;
            }

            _logger.LogInformation("開始移除舊版本 {Version}", version);
            Directory.Delete(versionDir, true);
            Console.WriteLine($"已移除舊版本 {version}");
        }

        // 取得版本資訊
        _logger.LogInformation("開始取得版本 {Version} 的遠端資訊", version);
        var versionList = await GetRemoteVersionsAsync();
        var versionInfo = versionList.FirstOrDefault(v => v.Version == version);

        if (versionInfo == null)
        {
            _logger.LogError("找不到版本資訊 {Version}", version);
            Console.WriteLine($"找不到版本 {version}");
            Console.WriteLine("使用 'uvm ls-remote' 查看可用版本");
            return;
        }

        _logger.LogInformation("開始安裝版本 {Version}，下載URL: {DownloadUrl}", version, versionInfo.DownloadUrl);
        Console.WriteLine($"正在安裝版本 {version}...");

        // 下載檔案
        _logger.LogInformation("開始下載版本 {Version}", version);
        Console.WriteLine("下載中...");
        await _downloadService.DownloadFileAsync(versionInfo.DownloadUrl, _settings.ZipFilePath);
        _logger.LogInformation("版本 {Version} 下載完成", version);
        
        // 檢查下載的檔案大小
        var downloadedFileInfo = new FileInfo(_settings.ZipFilePath);
        _logger.LogInformation("下載檔案大小: {Size} bytes ({SizeMB:F2} MB)", downloadedFileInfo.Length, downloadedFileInfo.Length / (1024.0 * 1024.0));
        Console.WriteLine($"下載完成，檔案大小: {downloadedFileInfo.Length / (1024.0 * 1024.0):F2} MB");

        // 驗證 SHA256
        if (!string.IsNullOrEmpty(versionInfo.Sha256))
        {
            _logger.LogInformation("開始驗證檔案完整性，期待 SHA256: {ExpectedHash}", versionInfo.Sha256);
            Console.WriteLine("驗證檔案完整性...");
            Console.WriteLine($"期待的 SHA256: {versionInfo.Sha256}");
            
            var actualHash = await _fileService.CalculateFileHashAsync(_settings.ZipFilePath);
            _logger.LogInformation("檔案實際 SHA256: {ActualHash}", actualHash);
            Console.WriteLine($"實際的 SHA256: {actualHash}");
            
            if (!await _fileService.VerifyFileHashAsync(_settings.ZipFilePath, versionInfo.Sha256))
            {
                _logger.LogError("檔案 SHA256 驗證失敗，期待: {ExpectedHash}，實際: {ActualHash}", versionInfo.Sha256, actualHash);
                Console.WriteLine($"❌ 檔案驗證失敗，安裝中止");
                Console.WriteLine($"期待的 SHA256: {versionInfo.Sha256}");
                Console.WriteLine($"實際的 SHA256: {actualHash}");
                Console.WriteLine("這可能表示：");
                Console.WriteLine("1. 檔案在傳輸過程中損壞");
                Console.WriteLine("2. 下載的不是正確的檔案（可能是 Google Drive 的錯誤頁面）");
                Console.WriteLine("3. 版本清單中的 SHA256 值不正確");
                return;
            }
            _logger.LogInformation("檔案完整性驗證通過");
            Console.WriteLine("✅ 檔案完整性驗證通過");
        }
        else
        {
            _logger.LogWarning("未提供 SHA256 值，跳過檔案完整性驗證");
            Console.WriteLine("⚠️ 未提供 SHA256 值，跳過檔案完整性驗證");
        }

        // 解壓縮
        if (Directory.Exists(_settings.TempExtractPath))
            Directory.Delete(_settings.TempExtractPath, true);

        _logger.LogInformation("開始解壓縮到臨時目錄: {TempPath}", _settings.TempExtractPath);
        Console.WriteLine("解壓縮中...");
        
        // 使用支援 UTF-8 編碼的解壓方式來處理中文檔案名稱
        using (var archive = ZipFile.OpenRead(_settings.ZipFilePath))
        {
            foreach (var entry in archive.Entries)
            {
                // 確保目標目錄存在
                var destinationPath = Path.Combine(_settings.TempExtractPath, entry.FullName);
                var destinationDir = Path.GetDirectoryName(destinationPath);
                
                if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }
                
                // 如果是檔案則解壓縮
                if (!string.IsNullOrEmpty(entry.Name))
                {
                    try
                    {
                        entry.ExtractToFile(destinationPath, overwrite: true);
                        _logger.LogDebug("解壓縮檔案: {FileName}", entry.FullName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "解壓縮檔案失敗: {FileName}", entry.FullName);
                        throw new Exception($"解壓縮檔案失敗: {entry.FullName} - {ex.Message}");
                    }
                }
            }
        }

        // 移動到版本目錄
        Directory.CreateDirectory(_settings.LocalBaseDir);
        Directory.Move(_settings.TempExtractPath, versionDir);

        _logger.LogInformation("版本 {Version} 安裝完成，安裝路徑: {InstallPath}", version, versionDir);
        Console.WriteLine($"✅ 版本 {version} 安裝完成");

        // 詢問是否要切換到新版本（互動式）
        Console.Write($"是否要切換到版本 {version}? (Y/n): ");
        var switchResponse = Console.ReadLine();
        if (switchResponse?.ToLower() != "n")
        {
            _logger.LogInformation("使用者選擇切換到版本 {Version}", version);
            await SetCurrentVersionAsync(version);
            _logger.LogInformation("已切換到版本 {Version}", version);
            Console.WriteLine($"已切換到版本 {version}");
        }
        else
        {
            _logger.LogInformation("使用者選擇不切換到版本 {Version}", version);
        }

        // 清理
        if (File.Exists(_settings.ZipFilePath))
        {
            File.Delete(_settings.ZipFilePath);
            _logger.LogDebug("清理下載檔案 {ZipFile}", _settings.ZipFilePath);
        }
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