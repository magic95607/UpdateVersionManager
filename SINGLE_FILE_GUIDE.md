# 單一檔案發佈指南

## 🎯 **單一檔案發佈完成！**

您的 UpdateVersionManager 現在支援打包為單一 exe 檔案，包含所有依賴項，無需安裝 .NET Runtime。

## ✅ **已實現的功能**

### 📦 **單一檔案特性**
- ✅ **完全自包含** - 包含 .NET 9 Runtime 和所有依賴
- ✅ **無需安裝** - 直接執行，無需額外安裝任何軟體
- ✅ **嵌入設定檔** - appsettings.json 打包在執行檔內
- ✅ **跨平台支援** - Windows、Linux、macOS (x64/ARM64)
- ✅ **程式碼最佳化** - 啟用 Trimming 和 ReadyToRun 編譯
- ✅ **完整功能** - 所有原有功能完全保留

### 🔧 **技術規格**
```xml
<!-- 專案設定 -->
<PublishSingleFile>true</PublishSingleFile>        <!-- 單一檔案發佈 -->
<SelfContained>true</SelfContained>                <!-- 自包含 Runtime -->
<PublishTrimmed>true</PublishTrimmed>               <!-- 移除未使用程式碼 -->
<TrimMode>partial</TrimMode>                       <!-- 部分裁剪模式 -->
<PublishReadyToRun>true</PublishReadyToRun>        <!-- 預編譯最佳化 -->
<IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
```

## 🚀 **如何建置單一檔案**

### **方法 1: 使用建置腳本**
```batch
REM Windows 批次檔案
.\build-single-file.bat

# PowerShell 腳本
.\build-single-file.ps1
```

### **方法 2: 手動命令**
```bash
# Windows x64
dotnet publish -c Release -r win-x64 --self-contained true -o "publish/win-x64" -p:PublishSingleFile=true -p:PublishTrimmed=true

# Linux x64
dotnet publish -c Release -r linux-x64 --self-contained true -o "publish/linux-x64" -p:PublishSingleFile=true -p:PublishTrimmed=true

# macOS x64
dotnet publish -c Release -r osx-x64 --self-contained true -o "publish/osx-x64" -p:PublishSingleFile=true -p:PublishTrimmed=true

# macOS ARM64 (Apple Silicon)
dotnet publish -c Release -r osx-arm64 --self-contained true -o "publish/osx-arm64" -p:PublishSingleFile=true -p:PublishTrimmed=true
```

### **方法 3: 使用 Publish Profile**
```bash
dotnet publish -p:PublishProfile=SingleFile -r win-x64
```

## 📊 **檔案大小比較**

| 版本類型 | 檔案數量 | 總大小 | 說明 |
|---------|---------|--------|------|
| **普通發佈** | ~50+ 檔案 | ~50MB | 需要所有 DLL 檔案 |
| **單一檔案** | 1 個 exe | ~31MB | 包含所有依賴的單一檔案 |
| **Framework-dependent** | ~10 檔案 | ~2MB | 需要預安裝 .NET 9 |

## 🎯 **使用方式**

### **部署到目標機器**
1. 複製對應平台的單一檔案：
   - `publish/win-x64/uvm.exe` (Windows)
   - `publish/linux-x64/uvm` (Linux)
   - `publish/osx-x64/uvm` (macOS Intel)
   - `publish/osx-arm64/uvm` (macOS Apple Silicon)

2. 直接執行，無需安裝任何依賴：
   ```bash
   # Windows
   uvm.exe help
   
   # Linux/macOS
   ./uvm help
   ```

### **整合到主程序**
單一檔案版本讓整合更簡單：

```csharp
// 主程序只需要一個檔案
var startInfo = new ProcessStartInfo
{
    FileName = "uvm.exe",  // 單一檔案，無其他依賴
    Arguments = "self-update",
    UseShellExecute = false,
    CreateNoWindow = true
};
```

## 🔧 **進階設定**

### **客製化建置選項**
修改 `UpdateVersionManager.csproj` 中的設定：

```xml
<PropertyGroup>
    <!-- 調整 Trimming 強度 -->
    <TrimMode>full</TrimMode>           <!-- 更激進的裁剪 -->
    
    <!-- 停用 ReadyToRun 以減小檔案大小 -->
    <PublishReadyToRun>false</PublishReadyToRun>
    
    <!-- 壓縮等級 -->
    <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
</PropertyGroup>
```

### **針對特定平台最佳化**
```bash
# 僅針對 Windows x64 最佳化
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:PublishTrimmed=true -p:OptimizationPreference=Size
```

## 💡 **部署建議**

### **企業環境部署**
- 使用單一檔案版本，避免依賴問題
- 放置在共享網路位置，供多台機器使用
- 設定版本管理和自動更新

### **便攜式應用**
- 單一檔案可直接放在 USB 隨身碟
- 無需管理員權限即可執行
- 適合技術支援和臨時使用

### **CI/CD 整合**
```yaml
# GitHub Actions 範例
- name: Build Single File
  run: dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true
- name: Upload Artifact
  uses: actions/upload-artifact@v3
  with:
    name: uvm-single-file
    path: publish/win-x64/uvm.exe
```

## 🎉 **完成效果**

現在您擁有：
- ✅ **完全獨立的執行檔** - 無需任何外部依賴
- ✅ **跨平台支援** - Windows、Linux、macOS 全支援
- ✅ **體積最佳化** - 透過 Trimming 減少檔案大小
- ✅ **效能最佳化** - ReadyToRun 提升啟動速度
- ✅ **易於部署** - 單一檔案即可完成部署
- ✅ **完整功能** - 所有原有功能完全保留

您的 UpdateVersionManager 現在是真正的「單一檔案應用程式」了！🚀
