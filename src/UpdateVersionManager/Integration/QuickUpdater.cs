using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace UpdateVersionManager.Integration;

/// <summary>
/// 主程序整合用的更新檢查器
/// 最少修改主程序的快速整合方案
/// </summary>
public static class QuickUpdater
{
    /// <summary>
    /// UpdateVersionManager 的路徑 (相對於主程序)
    /// </summary>
    public static string UpdaterPath { get; set; } = @"UpdateVersionManager\src\UpdateVersionManager";
    
    /// <summary>
    /// 快速檢查並更新 (異步版本)
    /// </summary>
    /// <param name="cleanOldVersion">是否清理舊版本</param>
    /// <param name="silent">是否靜默執行</param>
    /// <returns>更新結果</returns>
    public static async Task<UpdateResult> QuickUpdateAsync(bool cleanOldVersion = false, bool silent = false)
    {
        try
        {
            var args = "self-update";
            if (cleanOldVersion) args += " --clean";
            
            var result = await RunUpdaterAsync(args, silent);
            
            return new UpdateResult
            {
                Success = result.ExitCode == 0,
                Message = result.Output,
                ExitCode = result.ExitCode
            };
        }
        catch (Exception ex)
        {
            return new UpdateResult
            {
                Success = false,
                Message = $"更新失敗: {ex.Message}",
                ExitCode = -1
            };
        }
    }
    
    /// <summary>
    /// 快速檢查並更新 (同步版本)
    /// </summary>
    /// <param name="cleanOldVersion">是否清理舊版本</param>
    /// <param name="silent">是否靜默執行</param>
    /// <returns>更新結果</returns>
    public static UpdateResult QuickUpdate(bool cleanOldVersion = false, bool silent = false)
    {
        return QuickUpdateAsync(cleanOldVersion, silent).Result;
    }
    
    /// <summary>
    /// 檢查是否有可用更新 (不執行安裝)
    /// </summary>
    /// <returns>檢查結果</returns>
    public static async Task<CheckResult> CheckUpdateAsync()
    {
        try
        {
            var result = await RunUpdaterAsync("current", true);
            var currentOutput = result.Output;
            
            result = await RunUpdaterAsync("list-remote", true);
            var remoteOutput = result.Output;
            
            // 簡單解析 (實際使用時可能需要更複雜的解析)
            var hasUpdate = !remoteOutput.Contains("已是最新版本");
            
            return new CheckResult
            {
                Success = result.ExitCode == 0,
                HasUpdate = hasUpdate,
                CurrentVersion = ExtractCurrentVersion(currentOutput),
                Message = result.Output
            };
        }
        catch (Exception ex)
        {
            return new CheckResult
            {
                Success = false,
                HasUpdate = false,
                Message = $"檢查失敗: {ex.Message}"
            };
        }
    }
    
    private static async Task<ProcessResult> RunUpdaterAsync(string arguments, bool silent)
    {
        var updaterDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, UpdaterPath);
        
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run -- {arguments}",
            WorkingDirectory = updaterDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = silent
        };
        
        using var process = new Process { StartInfo = startInfo };
        process.Start();
        
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        
        await process.WaitForExitAsync();
        
        return new ProcessResult
        {
            ExitCode = process.ExitCode,
            Output = string.IsNullOrEmpty(error) ? output : $"{output}\n{error}"
        };
    }
    
    private static string? ExtractCurrentVersion(string output)
    {
        // 簡單的版本提取邏輯
        var lines = output.Split('\n');
        foreach (var line in lines)
        {
            if (line.Contains("當前版本:"))
            {
                var parts = line.Split(':');
                if (parts.Length > 1)
                {
                    return parts[1].Trim();
                }
            }
        }
        return null;
    }
}

/// <summary>
/// 更新結果
/// </summary>
public class UpdateResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ExitCode { get; set; }
}

/// <summary>
/// 檢查結果
/// </summary>
public class CheckResult
{
    public bool Success { get; set; }
    public bool HasUpdate { get; set; }
    public string? CurrentVersion { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 程序執行結果
/// </summary>
internal class ProcessResult
{
    public int ExitCode { get; set; }
    public string Output { get; set; } = string.Empty;
}
