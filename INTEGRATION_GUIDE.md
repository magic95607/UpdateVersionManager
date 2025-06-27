# 主程序自檢更新整合指南

本指南提供多種方式讓您的主程序整合 UpdateVersionManager 進行快速自檢更新。

## 📁 目錄結構說明

```
您的主程序/
├── YourMainApp.exe
├── UpdateVersionManager/          # 複製整個 UpdateVersionManager 到您的主程序目錄
│   ├── src/
│   │   └── UpdateVersionManager/  # 更新程序本體
│   ├── test/                      # 測試 (可選)
│   ├── quick-update.bat           # Windows 批次檔案
│   ├── quick-update.ps1           # PowerShell 腳本
│   └── INTEGRATION_GUIDE.md       # 本指南
└── ...其他檔案
```

## 🚀 方案 1：命令列調用 (零修改，推薦)

### 最簡單的整合方式
```bash
# 在主程序中執行
cd UpdateVersionManager/src/UpdateVersionManager
dotnet run -- self-update

# 或者使用批次檔案
UpdateVersionManager/quick-update.bat

# PowerShell 版本
UpdateVersionManager/quick-update.ps1
```

### 主程序修改範例 (C#)
```csharp
public static async Task CheckAndUpdateAsync()
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
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode == 0)
        {
            Console.WriteLine("✅ 更新檢查完成");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ 更新檢查失敗: {ex.Message}");
    }
}
```

## 🔧 方案 2：使用整合類別

### 複製 QuickUpdater.cs 到您的專案
將 `UpdateVersionManager/src/UpdateVersionManager/Integration/QuickUpdater.cs` 複製到您的主程序專案中。

### 使用範例
```csharp
using UpdateVersionManager.Integration;

// 異步更新
var result = await QuickUpdater.QuickUpdateAsync(cleanOldVersion: true);
if (result.Success)
{
    Console.WriteLine("✅ 更新成功");
}
else
{
    Console.WriteLine($"❌ 更新失敗: {result.Message}");
}

// 同步更新
var syncResult = QuickUpdater.QuickUpdate(cleanOldVersion: false);

// 僅檢查更新
var checkResult = await QuickUpdater.CheckUpdateAsync();
if (checkResult.HasUpdate)
{
    Console.WriteLine($"發現新版本，當前: {checkResult.CurrentVersion}");
}
```

## 🎯 方案 3：主程序啟動時自動檢查

### 在 Program.cs 中加入
```csharp
public static async Task Main(string[] args)
{
    // 程序啟動時檢查更新
    _ = Task.Run(async () =>
    {
        try
        {
            await Task.Delay(5000); // 延遲 5 秒，避免影響主程序啟動
            var result = await QuickUpdater.QuickUpdateAsync(silent: true);
            if (result.Success && result.Message.Contains("更新完成"))
            {
                // 可選：通知使用者有更新
                Console.WriteLine("🆙 應用程式已更新到最新版本");
            }
        }
        catch
        {
            // 靜默失敗，不影響主程序
        }
    });

    // 您的主程序邏輯
    // ...
}
```

## 📋 可用命令

### 基本命令
- `self-update` / `auto` / `quick-update` - 快速自檢並更新
- `self-update --clean` - 更新後清理舊版本

### 檢查命令
- `current` - 顯示當前版本
- `list-remote` - 列出可用版本
- `check` - 顯示詳細狀態

## 🛠️ 自訂設定

### 修改 UpdaterPath (方案 2)
```csharp
// 設定 UpdateVersionManager 的相對路徑
QuickUpdater.UpdaterPath = @"Tools\UpdateVersionManager\src\UpdateVersionManager";
```

### 批次檔案參數
```batch
quick-update.bat --clean --silent
```

### PowerShell 參數
```powershell
.\quick-update.ps1 -Clean -Silent
```

## 🔍 錯誤處理

### 常見問題
1. **路徑問題**: 確保 UpdateVersionManager 路徑正確
2. **權限問題**: 某些環境可能需要管理員權限建立符號連結
3. **網路問題**: 檢查 Google Drive 連線

### 錯誤碼
- `0` - 成功
- `非0` - 失敗 (檢查輸出訊息)

## 📊 整合效果

使用這些方案，您的主程序可以：
- ✅ 自動檢查更新
- ✅ 靜默下載安裝
- ✅ 無縫切換版本
- ✅ 最小化對主程序的影響
- ✅ 提供豐富的狀態回饋

選擇最適合您專案的整合方式即可！

## 📖 完整範例

請參考 `Examples/MainProgramIntegration.cs` 中的完整範例程式碼，其中包含：

1. **異步背景檢查更新** - 不阻塞主程序啟動
2. **靜默檢查更新** - 僅檢查不安裝
3. **強制立即更新** - 阻塞主程序執行更新

### 使用範例程式碼
```csharp
// 複製 Examples/MainProgramIntegration.cs 到您的專案
// 並根據需要調整路徑和邏輯

// 範例 1: 程序啟動時背景檢查
_ = Task.Run(async () => {
    var result = await QuickCheckUpdateAsync();
    // 處理結果...
});

// 範例 2: 檢查是否有更新
var hasUpdate = await HasUpdateAvailableAsync();
if (hasUpdate) {
    // 通知使用者或自動更新...
}

// 範例 3: 強制更新
var success = await ForceUpdateNowAsync();
```

## 🎯 建議的整合策略

### 對於桌面應用程式
- 使用**異步背景檢查**，在程序啟動 3-5 秒後執行
- 發現更新時顯示通知，讓使用者選擇是否重啟

### 對於服務程式
- 定期檢查更新（如每小時或每天）
- 維護模式期間自動更新

### 對於開發工具
- 每次啟動時快速檢查
- 提供手動更新命令

這樣的設計可以讓您的主程序獲得完善的自動更新能力！
