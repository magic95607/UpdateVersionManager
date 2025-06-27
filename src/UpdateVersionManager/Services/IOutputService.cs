using Microsoft.Extensions.Logging;
using UpdateVersionManager.Models;

namespace UpdateVersionManager.Services;

public interface IOutputService
{
    void WriteInfo(string message);
    void WriteError(string message);
    void WriteError(string message, Exception ex);
    void WriteWarning(string message);
    void WriteVerbose(string message);
    void WriteDebug(string message);
    void LogOnly(LogLevel level, string message);
    void LogOnly(LogLevel level, Exception ex, string message);
    void WriteConsoleOnly(string message);
    
    // 為測試添加的方法
    IReadOnlyList<string> GetCapturedOutput();
    void ClearCapturedOutput();
}
