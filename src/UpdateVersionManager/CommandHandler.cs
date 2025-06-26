using UpdateVersionManager.Services;

namespace UpdateVersionManager;

public static class CommandHandler
{
    public static async Task HandleCommand(string command, string[] parameters, VersionManager versionManager)
    {
        switch (command.ToLower())
        {
            case "list":
            case "ls":
                ListVersions(versionManager);
                break;
            case "list-remote":
            case "ls-remote":
                await ListRemoteVersions(versionManager);
                break;
            case "install":
                if (parameters.Length > 0)
                    await versionManager.InstallVersionAsync(parameters[0]);
                else
                    Console.WriteLine("使用方式: uvm install <version>");
                break;
            case "use":
                if (parameters.Length > 0)
                    await versionManager.UseVersionAsync(parameters[0]);
                else
                    Console.WriteLine("使用方式: uvm use <version>");
                break;
            case "current":
                ShowCurrentVersion(versionManager);
                break;
            case "update":
                await versionManager.AutoUpdateAsync();
                break;
            case "clean":
            case "uninstall":
                if (parameters.Length > 0)
                    await versionManager.CleanVersionAsync(parameters[0]);
                else
                    Console.WriteLine("使用方式: uvm clean <version>");
                break;
            case "hash":
            case "sha256":
                if (parameters.Length > 0)
                    await CalculateFileHash(parameters[0]);
                else
                    Console.WriteLine("使用方式: uvm hash <檔案路徑>");
                break;
            case "generate":
            case "gen":
                if (parameters.Length >= 3)
                    await GenerateVersionInfo(parameters[0], parameters[1], parameters[2]);
                else
                    Console.WriteLine("使用方式: uvm generate <版本號> <zip檔案路徑> <Google Drive檔案ID>");
                break;
            case "help":
            case "-h":
            case "--help":
                ShowHelp();
                break;
            case "check":
            case "info":
                ShowLinkInfo(versionManager);
                break;
            default:
                Console.WriteLine($"未知命令: {command}");
                ShowHelp();
                break;
        }
    }

    private static void ShowHelp()
    {
        Console.WriteLine("uvm - 版本管理工具 (Google Drive 版本)");
        Console.WriteLine();
        Console.WriteLine("命令:");
        Console.WriteLine("  update                              自動檢查並更新到最新版本");
        Console.WriteLine("  install <version>                   下載並安裝指定版本");
        Console.WriteLine("  list, ls                            列出所有已安裝的版本");
        Console.WriteLine("  list-remote, ls-remote              列出所有可用的遠端版本");
        Console.WriteLine("  use <version>                       切換到指定版本");
        Console.WriteLine("  current                             顯示當前使用的版本");
        Console.WriteLine("  clean <version>                     刪除指定版本");
        Console.WriteLine("  hash <檔案路徑>                     計算檔案的 SHA256");
        Console.WriteLine("  generate <版本> <zip檔> <檔案ID>     產生版本資訊");
        Console.WriteLine("  check, info                     顯示當前連結資訊");
        Console.WriteLine("  help                                顯示此幫助訊息");
    }

    private static void ListVersions(VersionManager versionManager)
    {
        Console.WriteLine("已安裝的版本:");
        var versions = versionManager.GetInstalledVersions();
        var currentVersion = versionManager.GetCurrentVersion();

        if (!versions.Any())
        {
            Console.WriteLine("  (無已安裝版本)");
            return;
        }

        foreach (var version in versions)
        {
            var marker = version == currentVersion ? " (current)" : "";
            Console.WriteLine($"  {version}{marker}");
        }
    }

    private static async Task ListRemoteVersions(VersionManager versionManager)
    {
        try
        {
            Console.WriteLine("正在取得遠端版本清單...");
            var versionList = await versionManager.GetRemoteVersionsAsync();

            if (!versionList.Any())
            {
                Console.WriteLine("無可用版本");
                return;
            }

            Console.WriteLine("可用版本:");
            var currentVersion = versionManager.GetCurrentVersion();
            var installedVersions = versionManager.GetInstalledVersions().ToHashSet();

            foreach (var version in versionList.OrderByDescending(v => v.Version))
            {
                var status = "";
                if (version.Version == currentVersion)
                    status = " (current)";
                else if (installedVersions.Contains(version.Version))
                    status = " (installed)";

                Console.WriteLine($"  {version.Version,-12} {version.ReleaseDate,-12} {version.Description}{status}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"取得遠端版本清單失敗: {ex.Message}");
        }
    }

    private static void ShowCurrentVersion(VersionManager versionManager)
    {
        var current = versionManager.GetCurrentVersion();
        if (current != null)
            Console.WriteLine($"當前版本: {current}");
        else
            Console.WriteLine("尚未設定版本");
    }

    private static async Task CalculateFileHash(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"檔案不存在: {filePath}");
            return;
        }

        try
        {
            var fileService = new FileService();
            Console.WriteLine($"計算檔案 SHA256: {filePath}");
            var hash = await fileService.CalculateFileHashAsync(filePath);

            Console.WriteLine($"SHA256: {hash}");
            Console.WriteLine($"檔案大小: {new FileInfo(filePath).Length:N0} bytes");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"計算 SHA256 失敗: {ex.Message}");
        }
    }

    private static void ShowLinkInfo(VersionManager versionManager)
    {
        Console.WriteLine("=== 當前狀態 ===");

        var currentVersion = versionManager.GetCurrentVersion();
        Console.WriteLine($"當前版本: {currentVersion ?? "未設定"}");

        var symbolicLinkService = new SymbolicLinkService(new FileService());
        symbolicLinkService.ShowLinkInfo("current");

        Console.WriteLine("\n=== 已安裝版本 ===");
        var installedVersions = versionManager.GetInstalledVersions();
        foreach (var version in installedVersions)
        {
            var marker = version == currentVersion ? " (current)" : "";
            Console.WriteLine($"  {version}{marker}");
        }
    }

    private static async Task GenerateVersionInfo(string version, string zipFilePath, string googleDriveFileId)
    {
        try
        {
            var fileService = new FileService();
            var versionInfo = await fileService.GenerateVersionInfoAsync(version, zipFilePath, googleDriveFileId);
            await fileService.SaveVersionInfoToJsonAsync(versionInfo);

            Console.WriteLine("✅ 版本資訊已產生:");
            Console.WriteLine($"   版本: {versionInfo.Version}");
            Console.WriteLine($"   SHA256: {versionInfo.Sha256}");
            Console.WriteLine($"   大小: {versionInfo.Size:N0} bytes");
            Console.WriteLine($"   檔案: version_item.json");
            Console.WriteLine();
            Console.WriteLine("請將此項目加入到 versions.json 的 versions 陣列中");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"產生版本資訊失敗: {ex.Message}");
        }
    }
}