using Microsoft.Extensions.Logging;
using UpdateVersionManager.Models;

namespace UpdateVersionManager.Services;

public class OutputService
{
    private readonly ILogger<OutputService> _logger;
    private readonly UpdateVersionManagerSettings _settings;

    public OutputService(ILogger<OutputService> logger, UpdateVersionManagerSettings settings)
    {
        _logger = logger;
        _settings = settings;
    }

    /// <summary>
    /// 輸出用戶訊息，永遠顯示在 Console，也記錄到 Log
    /// </summary>
    public void WriteInfo(string message)
    {
        Console.WriteLine(message);
        _logger.LogInformation(message);
    }

    /// <summary>
    /// 輸出錯誤訊息，永遠顯示在 Console，也記錄到 Log
    /// </summary>
    public void WriteError(string message)
    {
        Console.WriteLine(message);
        _logger.LogError(message);
    }

    /// <summary>
    /// 輸出錯誤訊息（含例外），永遠顯示在 Console，也記錄到 Log
    /// </summary>
    public void WriteError(string message, Exception ex)
    {
        Console.WriteLine($"{message}: {ex.Message}");
        _logger.LogError(ex, message);
    }

    /// <summary>
    /// 輸出警告訊息，永遠顯示在 Console，也記錄到 Log
    /// </summary>
    public void WriteWarning(string message)
    {
        Console.WriteLine(message);
        _logger.LogWarning(message);
    }

    /// <summary>
    /// 輸出詳細資訊，只有在 VerboseOutput=true 時才顯示在 Console，但永遠記錄到 Log
    /// </summary>
    public void WriteVerbose(string message)
    {
        if (_settings.VerboseOutput)
        {
            Console.WriteLine($"[VERBOSE] {message}");
        }
        _logger.LogInformation(message);
    }

    /// <summary>
    /// 輸出除錯資訊，只有在 VerboseOutput=true 時才顯示在 Console，記錄為 Debug 層級到 Log
    /// </summary>
    public void WriteDebug(string message)
    {
        if (_settings.VerboseOutput)
        {
            Console.WriteLine($"[DEBUG] {message}");
        }
        _logger.LogDebug(message);
    }

    /// <summary>
    /// 只記錄到 Log，不顯示在 Console
    /// </summary>
    public void LogOnly(LogLevel level, string message)
    {
        _logger.Log(level, message);
    }

    /// <summary>
    /// 只記錄到 Log，不顯示在 Console（含例外）
    /// </summary>
    public void LogOnly(LogLevel level, Exception ex, string message)
    {
        _logger.Log(level, ex, message);
    }

    /// <summary>
    /// 只輸出到 Console，不記錄到 Log（用於純用戶介面顯示）
    /// </summary>
    public virtual void WriteConsoleOnly(string message)
    {
        Console.WriteLine(message);
    }
}
