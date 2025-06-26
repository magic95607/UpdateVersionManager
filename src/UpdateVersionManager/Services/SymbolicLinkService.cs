using System.Diagnostics;

namespace UpdateVersionManager.Services;

public class SymbolicLinkService
{
    private readonly FileService _fileService;

    public SymbolicLinkService(FileService fileService)
    {
        _fileService = fileService;
    }

    public async Task<bool> CreateSymbolicLinkAsync(string linkPath, string targetPath)
    {
        try
        {
            Console.WriteLine($"[SymbolicLink] 嘗試建立符號連結:");
            Console.WriteLine($"[SymbolicLink]   來源: {Path.GetFullPath(targetPath)}");
            Console.WriteLine($"[SymbolicLink]   目標: {linkPath}");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c mklink /D \"{linkPath}\" \"{Path.GetFullPath(targetPath)}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                Console.WriteLine($"[SymbolicLink] 符號連結建立成功");
                if (!string.IsNullOrEmpty(output))
                    Console.WriteLine($"[SymbolicLink] 輸出: {output.Trim()}");
                return true;
            }
            else
            {
                Console.WriteLine($"[SymbolicLink] 符號連結建立失敗 (退出碼: {process.ExitCode})");
                if (!string.IsNullOrEmpty(output))
                    Console.WriteLine($"[SymbolicLink] 輸出: {output.Trim()}");
                if (!string.IsNullOrEmpty(error))
                    Console.WriteLine($"[SymbolicLink] 錯誤: {error.Trim()}");

                // 檢查常見的錯誤原因
                if (error.Contains("需要提高的權限") || error.Contains("privilege"))
                {
                    Console.WriteLine("[SymbolicLink] 提示: 需要管理員權限才能建立符號連結");
                }

                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SymbolicLink] 建立符號連結時發生例外: {ex.Message}");
            return false;
        }
    }

    public async Task UpdateAppLinkAsync(string version, string versionDir, string linkPath)
    {
        Console.WriteLine($"[Updater] 更新應用程式連結到版本 {version}");
        Console.WriteLine($"[Updater] 版本目錄: {versionDir}");
        Console.WriteLine($"[Updater] 連結路徑: {linkPath}");

        // 檢查版本目錄是否存在
        if (!Directory.Exists(versionDir))
        {
            Console.WriteLine($"[Updater] 錯誤: 版本目錄不存在 {versionDir}");
            return;
        }

        // 移除舊的捷徑
        if (Directory.Exists(linkPath))
        {
            Console.WriteLine("[Updater] 發現現有的 current 目錄，正在移除...");

            if (IsSymbolicLink(linkPath))
            {
                Console.WriteLine("[Updater] 移除符號連結");
                Directory.Delete(linkPath);
            }
            else
            {
                Console.WriteLine("[Updater] 移除複製的資料夾");
                Directory.Delete(linkPath, true);
            }
        }
        else
        {
            Console.WriteLine("[Updater] 首次建立 current 目錄");
        }

        // 嘗試建立符號連結
        if (await CreateSymbolicLinkAsync(linkPath, versionDir))
        {
            Console.WriteLine($"[Updater] ✅ 已建立符號連結 current -> {version}");
        }
        else
        {
            // fallback: 複製資料夾
            Console.WriteLine("[Updater] ⚠️  無法建立符號連結，改用複製資料夾");
            Console.WriteLine("[Updater] 正在複製檔案...");

            try
            {
                _fileService.CopyDirectory(versionDir, linkPath);
                Console.WriteLine($"[Updater] ✅ 已複製版本 {version} 到 current 目錄");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Updater] ❌ 複製資料夾失敗: {ex.Message}");
                throw;
            }
        }

        // 驗證結果
        if (Directory.Exists(linkPath))
        {
            var linkType = IsSymbolicLink(linkPath) ? "符號連結" : "複製的資料夾";
            Console.WriteLine($"[Updater] 驗證成功: current 目錄已建立 ({linkType})");

            // 列出目錄內容以確認
            var files = Directory.GetFiles(linkPath);
            Console.WriteLine($"[Updater] current 目錄包含 {files.Length} 個檔案");
        }
        else
        {
            Console.WriteLine("[Updater] ❌ 錯誤: current 目錄建立失敗");
        }
    }

    private bool IsSymbolicLink(string path)
    {
        try
        {
            var dirInfo = new DirectoryInfo(path);
            return dirInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
        }
        catch
        {
            return false;
        }
    }

    public void ShowLinkInfo(string linkPath)
    {
        if (!Directory.Exists(linkPath))
        {
            Console.WriteLine($"目錄不存在: {linkPath}");
            return;
        }

        var isSymLink = IsSymbolicLink(linkPath);
        Console.WriteLine($"目錄類型: {(isSymLink ? "符號連結" : "一般資料夾")}");

        if (isSymLink)
        {
            try
            {
                var dirInfo = new DirectoryInfo(linkPath);
                Console.WriteLine($"連結目標: {dirInfo.ResolveLinkTarget(true)?.FullName ?? "無法解析"}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"無法取得連結目標: {ex.Message}");
            }
        }
    }
}