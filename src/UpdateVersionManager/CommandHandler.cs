using UpdateVersionManager.Services;

namespace UpdateVersionManager;

public static class CommandHandler
{
    public static async Task HandleCommand(string command, string[] parameters, Services.VersionManager versionManager, OutputService output)
    {
        output.WriteDebug($"處理命令: {command} {string.Join(" ", parameters)}");
        
        switch (command.ToLower())
        {
            case "list":
            case "ls":
                ListVersions(versionManager, output);
                break;
            case "list-remote":
            case "ls-remote":
                await ListRemoteVersions(versionManager, output);
                break;
            case "install":
                if (parameters.Length > 0)
                    await versionManager.InstallVersionAsync(parameters[0]);
                else
                    output.WriteConsoleOnly("使用方式: uvm install <version>");
                break;
            case "use":
                if (parameters.Length > 0)
                    await versionManager.UseVersionAsync(parameters[0]);
                else
                    output.WriteConsoleOnly("使用方式: uvm use <version>");
                break;
            case "current":
                ShowCurrentVersion(versionManager, output);
                break;
            case "update":
                await versionManager.AutoUpdateAsync();
                break;
            case "clean":
            case "uninstall":
                if (parameters.Length > 0)
                    await versionManager.CleanVersionAsync(parameters[0]);
                else
                    output.WriteConsoleOnly("使用方式: uvm clean <version>");
                break;
            case "hash":
            case "sha256":
                if (parameters.Length > 0)
                    await CalculateFileHash(parameters[0], output);
                else
                    output.WriteConsoleOnly("使用方式: uvm hash <檔案路徑>");
                break;
            case "generate":
            case "gen":
                if (parameters.Length >= 3)
                    await GenerateVersionInfo(parameters[0], parameters[1], parameters[2], output);
                else
                    output.WriteConsoleOnly("使用方式: uvm generate <版本號> <zip檔案路徑> <Google Drive檔案ID>");
                break;
            case "help":
            case "-h":
            case "--help":
                ShowHelp(output);
                break;
            case "check":
            case "info":
                ShowLinkInfo(versionManager, output);
                break;
            default:
                output.WriteConsoleOnly($"未知命令: {command}");
                ShowHelp(output);
                break;
        }
    }

    private static void ShowHelp(OutputService output)
    {
        output.WriteConsoleOnly("uvm - 版本管理工具 (Google Drive 版本)");
        output.WriteConsoleOnly("");
        output.WriteConsoleOnly("命令:");
        output.WriteConsoleOnly("  update                              自動檢查並更新到最新版本");
        output.WriteConsoleOnly("  install <version>                   下載並安裝指定版本");
        output.WriteConsoleOnly("  list, ls                            列出所有已安裝的版本");
        output.WriteConsoleOnly("  list-remote, ls-remote              列出所有可用的遠端版本");
        output.WriteConsoleOnly("  use <version>                       切換到指定版本");
        output.WriteConsoleOnly("  current                             顯示當前使用的版本");
        output.WriteConsoleOnly("  clean <version>                     刪除指定版本");
        output.WriteConsoleOnly("  hash <檔案路徑>                     計算檔案的 SHA256");
        output.WriteConsoleOnly("  generate <版本> <zip檔> <檔案ID>     產生版本資訊");
        output.WriteConsoleOnly("  check, info                     顯示當前連結資訊");
        output.WriteConsoleOnly("  help                                顯示此幫助訊息");
    }

    private static void ListVersions(Services.VersionManager versionManager, OutputService output)
    {
        output.WriteConsoleOnly("已安裝的版本:");
        var versions = versionManager.GetInstalledVersions();
        var currentVersion = versionManager.GetCurrentVersion();

        if (!versions.Any())
        {
            output.WriteConsoleOnly("  (無已安裝版本)");
            return;
        }

        foreach (var version in versions)
        {
            var marker = version == currentVersion ? " (current)" : "";
            output.WriteConsoleOnly($"  {version}{marker}");
        }
    }

    private static async Task ListRemoteVersions(VersionManager versionManager, OutputService output)
    {
        try
        {
            output.WriteVerbose("開始取得遠端版本清單");
            output.WriteConsoleOnly("正在取得遠端版本清單...");
            var versionList = await versionManager.GetRemoteVersionsAsync();

            if (!versionList.Any())
            {
                output.WriteWarning("無可用版本");
                return;
            }

            output.WriteVerbose($"取得到 {versionList.Count} 個版本");
            output.WriteConsoleOnly("可用版本:");
            var currentVersion = versionManager.GetCurrentVersion();
            var installedVersions = versionManager.GetInstalledVersions().ToHashSet();

            foreach (var version in versionList.OrderByDescending(v => v.Version))
            {
                var status = "";
                if (version.Version == currentVersion)
                    status = " (current)";
                else if (installedVersions.Contains(version.Version))
                    status = " (installed)";

                output.WriteConsoleOnly($"  {version.Version,-12} {version.ReleaseDate,-12} {version.Description}{status}");
            }
        }
        catch (Exception ex)
        {
            output.WriteError("取得遠端版本清單失敗", ex);
        }
    }

    private static void ShowCurrentVersion(VersionManager versionManager, OutputService output)
    {
        var current = versionManager.GetCurrentVersion();
        if (current != null)
            output.WriteConsoleOnly($"當前版本: {current}");
        else
            output.WriteConsoleOnly("尚未設定版本");
    }

    private static async Task CalculateFileHash(string filePath, OutputService output)
    {
        if (!File.Exists(filePath))
        {
            output.WriteWarning($"檔案不存在: {filePath}");
            return;
        }

        try
        {
            var fileService = new FileService();
            output.WriteVerbose($"開始計算檔案 SHA256: {filePath}");
            output.WriteConsoleOnly($"計算檔案 SHA256: {filePath}");
            var hash = await fileService.CalculateFileHashAsync(filePath);

            var fileInfo = new FileInfo(filePath);
            output.WriteVerbose($"檔案 SHA256 計算完成: {hash}, 大小: {fileInfo.Length} bytes");
            output.WriteConsoleOnly($"SHA256: {hash}");
            output.WriteConsoleOnly($"檔案大小: {fileInfo.Length:N0} bytes");
        }
        catch (Exception ex)
        {
            output.WriteError($"計算 SHA256 失敗", ex);
        }
    }

    private static void ShowLinkInfo(VersionManager versionManager, OutputService output)
    {
        output.WriteConsoleOnly("=== 當前狀態 ===");

        var currentVersion = versionManager.GetCurrentVersion();
        output.WriteConsoleOnly($"當前版本: {currentVersion ?? "未設定"}");

        // 建立簡單的連結檢查
        var linkPath = Path.Combine(Directory.GetCurrentDirectory(), "current");
        if (Directory.Exists(linkPath))
        {
            var isSymLink = new DirectoryInfo(linkPath).Attributes.HasFlag(FileAttributes.ReparsePoint);
            output.WriteConsoleOnly($"目錄類型: {(isSymLink ? "符號連結" : "一般資料夾")}");
            
            if (isSymLink)
            {
                try
                {
                    var dirInfo = new DirectoryInfo(linkPath);
                    output.WriteConsoleOnly($"連結目標: {dirInfo.ResolveLinkTarget(true)?.FullName ?? "無法解析"}");
                }
                catch (Exception ex)
                {
                    output.WriteConsoleOnly($"無法取得連結目標: {ex.Message}");
                }
            }
        }
        else
        {
            output.WriteConsoleOnly("目錄不存在: current");
        }

        output.WriteConsoleOnly("\n=== 已安裝版本 ===");
        var installedVersions = versionManager.GetInstalledVersions();
        foreach (var version in installedVersions)
        {
            var marker = version == currentVersion ? " (current)" : "";
            output.WriteConsoleOnly($"  {version}{marker}");
        }
    }

    private static async Task GenerateVersionInfo(string version, string zipFilePath, string googleDriveFileId, OutputService output)
    {
        try
        {
            var fileService = new FileService();
            var versionInfo = await fileService.GenerateVersionInfoAsync(version, zipFilePath, googleDriveFileId);
            await fileService.SaveVersionInfoToJsonAsync(versionInfo);

            output.WriteConsoleOnly("✅ 版本資訊已產生:");
            output.WriteConsoleOnly($"   版本: {versionInfo.Version}");
            output.WriteConsoleOnly($"   SHA256: {versionInfo.Sha256}");
            output.WriteConsoleOnly($"   大小: {versionInfo.Size:N0} bytes");
            output.WriteConsoleOnly($"   檔案: version_item.json");
            output.WriteConsoleOnly("");
            output.WriteConsoleOnly("請將此項目加入到 versions.json 的 versions 陣列中");
        }
        catch (Exception ex)
        {
            output.WriteError("產生版本資訊失敗", ex);
        }
    }
}