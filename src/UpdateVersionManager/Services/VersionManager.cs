using System.IO.Compression;
using System.Text.Json;
using UpdateVersionManager.Models;

namespace UpdateVersionManager.Services;

public class VersionManager
{
    private readonly GoogleDriveService _googleDriveService;
    private readonly FileService _fileService;
    private readonly SymbolicLinkService _symbolicLinkService;

    // 設定常數
    private const string GoogleDriveVersionListFileId = "1HaA7rtbn_t7LWH67Pfr-tMV7cT7w7-E2"; // Google Drive 上的版本列表檔案 ID
    private const string LocalBaseDir = "app_versions";
    private const string CurrentVersionFile = "current_version.txt";
    private const string TempExtractPath = "temp_update";
    private const string ZipFilePath = "update.zip";
    private const string AppLinkName = "current";

    private readonly string _versionListUrl = $"https://drive.google.com/uc?export=download&id={GoogleDriveVersionListFileId}";

    public VersionManager()
    {
        _googleDriveService = new GoogleDriveService();
        _fileService = new FileService();
        _symbolicLinkService = new SymbolicLinkService(_fileService);
    }

    public string? GetCurrentVersion()
    {
        if (File.Exists(CurrentVersionFile))
            return File.ReadAllText(CurrentVersionFile).Trim();
        return null;
    }

    public List<string> GetInstalledVersions()
    {
        if (!Directory.Exists(LocalBaseDir))
            return new List<string>();

        return Directory.GetDirectories(LocalBaseDir)
            .Select(d => Path.GetFileName(d))
            .OrderByDescending(v => v)
            .ToList();
    }

    public async Task<List<VersionInfo>> GetRemoteVersionsAsync()
    {
        try
        {
            var json = await _googleDriveService.DownloadTextAsync(_versionListUrl);

            // 調試：輸出原始 JSON
            Console.WriteLine("取得的 JSON 內容:");
            Console.WriteLine(json);
            Console.WriteLine("--- JSON 結束 ---");

            var versionListData = JsonDocument.Parse(json).RootElement;
            var versions = new List<VersionInfo>();

            // 檢查 JSON 結構
            if (!versionListData.TryGetProperty("versions", out var versionsArray))
            {
                Console.WriteLine("錯誤: JSON 中找不到 'versions' 屬性");
                Console.WriteLine("可用的屬性:");
                foreach (var property in versionListData.EnumerateObject())
                {
                    Console.WriteLine($"  - {property.Name}");
                }
                return versions;
            }

            foreach (var versionElement in versionsArray.EnumerateArray())
            {
                try
                {
                    // 支援兩種命名格式：camelCase 和 PascalCase
                    var version = GetStringProperty(versionElement, "version", "Version");
                    var downloadUrl = GetStringProperty(versionElement, "downloadUrl", "DownloadUrl");

                    if (string.IsNullOrEmpty(version))
                    {
                        Console.WriteLine("警告: 版本項目缺少 'version' 或 'Version' 欄位，跳過");
                        continue;
                    }

                    if (string.IsNullOrEmpty(downloadUrl))
                    {
                        Console.WriteLine($"警告: 版本 {version} 缺少 'downloadUrl' 或 'DownloadUrl' 欄位，跳過");
                        continue;
                    }

                    var versionInfo = new VersionInfo
                    {
                        Version = version,
                        DownloadUrl = downloadUrl,
                        Sha256 = GetStringProperty(versionElement, "sha256", "Sha256"),
                        Size = GetLongProperty(versionElement, "size", "Size"),
                        ReleaseDate = GetStringProperty(versionElement, "releaseDate", "ReleaseDate") ?? "",
                        Description = GetStringProperty(versionElement, "description", "Description") ?? ""
                    };

                    versions.Add(versionInfo);
                    Console.WriteLine($"成功解析版本: {versionInfo.Version}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"警告: 解析版本項目時發生錯誤: {ex.Message}");
                }
            }

            return versions;
        }
        catch (JsonException ex)
        {
            throw new Exception($"JSON 解析錯誤: {ex.Message}");
        }
        catch (Exception ex)
        {
            throw new Exception($"取得遠端版本失敗: {ex.Message}");
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
        var versionDir = Path.Combine(LocalBaseDir, version);
        if (!Directory.Exists(versionDir))
        {
            Console.WriteLine($"版本 {version} 不存在");
            Console.WriteLine("使用 'uvm list' 查看可用版本");
            return;
        }

        await SetCurrentVersionAsync(version);
        Console.WriteLine($"已切換至版本 {version}");
    }

    public async Task CleanVersionAsync(string version)
    {
        var versionDir = Path.Combine(LocalBaseDir, version);
        if (!Directory.Exists(versionDir))
        {
            Console.WriteLine($"版本 {version} 不存在");
            return;
        }

        var currentVersion = GetCurrentVersion();
        if (version == currentVersion)
        {
            Console.WriteLine($"無法刪除當前使用的版本 {version}");
            return;
        }

        Directory.Delete(versionDir, true);
        Console.WriteLine($"已刪除版本 {version}");
    }

    public async Task AutoUpdateAsync()
    {
        try
        {
            Console.WriteLine("[Updater] 檢查最新版本...");
            var versionList = await GetRemoteVersionsAsync();

            if (!versionList.Any())
            {
                Console.WriteLine("[Updater] 無法取得版本資訊");
                return;
            }

            var latestVersion = versionList.OrderByDescending(v => v.Version).First();
            var currentVersion = GetCurrentVersion();

            Console.WriteLine($"[Updater] 最新版本: {latestVersion.Version}");
            Console.WriteLine($"[Updater] 當前版本: {currentVersion}");

            if (latestVersion.Version == currentVersion)
            {
                Console.WriteLine("[Updater] 已是最新版本");
                return;
            }

            Console.WriteLine($"[Updater] 發現新版本 {latestVersion.Version}，開始自動更新...");
            // 呼叫專門的自動安裝方法，不需要用戶互動
            await InstallVersionSilentAsync(latestVersion.Version);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Updater] 更新失敗: {ex.Message}");
        }
    }

    // 新增一個靜默安裝方法，專門給自動更新使用
    private async Task InstallVersionSilentAsync(string version)
    {
        var versionDir = Path.Combine(LocalBaseDir, version);

        // 檢查版本是否已安裝
        if (Directory.Exists(versionDir))
        {
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
            Console.WriteLine($"[Updater] ❌ 找不到版本 {version}");
            return;
        }

        Console.WriteLine($"[Updater] 正在下載版本 {version}...");

        // 下載檔案
        await _googleDriveService.DownloadFileAsync(versionInfo.DownloadUrl, ZipFilePath);

        // 驗證 SHA256
        if (!string.IsNullOrEmpty(versionInfo.Sha256))
        {
            Console.WriteLine("[Updater] 驗證檔案完整性...");
            if (!await _fileService.VerifyFileHashAsync(ZipFilePath, versionInfo.Sha256))
            {
                Console.WriteLine("[Updater] ❌ 檔案驗證失敗，安裝中止");
                return;
            }
        }

        // 解壓縮
        if (Directory.Exists(TempExtractPath))
            Directory.Delete(TempExtractPath, true);

        Console.WriteLine("[Updater] 解壓縮中...");
        ZipFile.ExtractToDirectory(ZipFilePath, TempExtractPath);

        // 移動到版本目錄
        Directory.CreateDirectory(LocalBaseDir);
        Directory.Move(TempExtractPath, versionDir);

        Console.WriteLine($"[Updater] 版本 {version} 安裝完成");

        // 自動切換到新版本（無需確認）
        await SetCurrentVersionAsync(version);
        Console.WriteLine($"[Updater] ✅ 已自動切換到版本 {version}");

        // 清理
        if (File.Exists(ZipFilePath))
            File.Delete(ZipFilePath);
    }

    // 修改原有的 InstallVersionAsync 方法，保持互動式安裝的行為
    public async Task InstallVersionAsync(string version)
    {
        var versionDir = Path.Combine(LocalBaseDir, version);

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
        await _googleDriveService.DownloadFileAsync(versionInfo.DownloadUrl, ZipFilePath);

        // 驗證 SHA256
        if (!string.IsNullOrEmpty(versionInfo.Sha256))
        {
            Console.WriteLine("驗證檔案完整性...");
            if (!await _fileService.VerifyFileHashAsync(ZipFilePath, versionInfo.Sha256))
            {
                Console.WriteLine("檔案驗證失敗，安裝中止");
                return;
            }
        }

        // 解壓縮
        if (Directory.Exists(TempExtractPath))
            Directory.Delete(TempExtractPath, true);

        Console.WriteLine("解壓縮中...");
        ZipFile.ExtractToDirectory(ZipFilePath, TempExtractPath);

        // 移動到版本目錄
        Directory.CreateDirectory(LocalBaseDir);
        Directory.Move(TempExtractPath, versionDir);

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
        if (File.Exists(ZipFilePath))
            File.Delete(ZipFilePath);
    }

    private async Task SetCurrentVersionAsync(string version)
    {
        // 更新版本記錄
        await File.WriteAllTextAsync(CurrentVersionFile, version);

        // 更新捷徑資料夾
        var versionDir = Path.Combine(LocalBaseDir, version);
        var linkPath = Path.Combine(Directory.GetCurrentDirectory(), AppLinkName);
        await _symbolicLinkService.UpdateAppLinkAsync(version, versionDir, linkPath);
    }
}