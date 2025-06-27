using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MainApp.Example;

/// <summary>
/// 主程序整合 UpdateVersionManager 的範例
/// </summary>
public class MainProgram
{
    /// <summary>
    /// 主程序進入點範例
    /// </summary>
    public static async Task Main(string[] args)
    {
        Console.WriteLine("主程序啟動中...");

        // 方案 1: 程序啟動時異步檢查更新 (不阻塞主程序)
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(3000); // 延遲 3 秒，讓主程序先啟動
                Console.WriteLine("🔍 背景檢查更新中...");
                
                var updateResult = await QuickCheckUpdateAsync();
                if (updateResult.HasUpdate)
                {
                    Console.WriteLine($"🆙 發現新版本，建議重新啟動程序");
                }
                else if (updateResult.Success)
                {
                    Console.WriteLine("✅ 已是最新版本");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新檢查失敗: {ex.Message}");
            }
        });

        // 您的主程序邏輯
        Console.WriteLine("主程序運行中...");
        
        // 模擬主程序工作
        for (int i = 0; i < 10; i++)
        {
            Console.WriteLine($"主程序工作 {i + 1}/10");
            await Task.Delay(1000);
        }

        Console.WriteLine("主程序結束");
    }

    /// <summary>
    /// 方案 1: 透過命令列快速檢查更新
    /// </summary>
    public static async Task<(bool Success, bool HasUpdate, string Message)> QuickCheckUpdateAsync()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "run -- self-update",
                WorkingDirectory = @"UpdateVersionManager\src\UpdateVersionManager",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
                return (false, false, "無法啟動更新程序");

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            var success = process.ExitCode == 0;
            var hasUpdate = output.Contains("更新完成") || output.Contains("發現新版本");
            var message = string.IsNullOrEmpty(error) ? output : $"{output}\n{error}";

            return (success, hasUpdate, message);
        }
        catch (Exception ex)
        {
            return (false, false, $"檢查更新失敗: {ex.Message}");
        }
    }

    /// <summary>
    /// 方案 2: 靜默檢查是否有更新 (不執行安裝)
    /// </summary>
    public static async Task<bool> HasUpdateAvailableAsync()
    {
        try
        {
            // 先檢查當前版本
            var currentResult = await RunUpdaterCommandAsync("current");
            if (!currentResult.Success)
                return false;

            // 再檢查遠端版本
            var remoteResult = await RunUpdaterCommandAsync("list-remote");
            if (!remoteResult.Success)
                return false;

            // 簡單的版本比較邏輯 (您可以根據需要改進)
            return !remoteResult.Output.Contains("已是最新版本");
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 執行 UpdateVersionManager 命令的通用方法
    /// </summary>
    private static async Task<(bool Success, string Output)> RunUpdaterCommandAsync(string command)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run -- {command}",
                WorkingDirectory = @"UpdateVersionManager\src\UpdateVersionManager",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
                return (false, "無法啟動程序");

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            var result = string.IsNullOrEmpty(error) ? output : $"{output}\n{error}";
            return (process.ExitCode == 0, result);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// 方案 3: 強制立即更新 (會阻塞主程序)
    /// </summary>
    public static async Task<bool> ForceUpdateNowAsync()
    {
        try
        {
            Console.WriteLine("🔄 正在執行強制更新...");
            
            var result = await RunUpdaterCommandAsync("self-update --clean");
            
            if (result.Success)
            {
                Console.WriteLine("✅ 更新完成！");
                Console.WriteLine("⚠️  建議重新啟動程序以使用新版本");
                return true;
            }
            else
            {
                Console.WriteLine($"❌ 更新失敗: {result.Output}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 更新過程中發生錯誤: {ex.Message}");
            return false;
        }
    }
}
