# UpdateVersionManager

基於 Google Drive 的版本管理工具，支援自動更新和版本切換功能。

## 快速開始

### 下載預編譯版本

從 [GitHub Releases](https://github.com/magic95607/UpdateVersionManager/releases/latest) 下載對應您作業系統的預編譯版本：

- **Windows**: `uvm-win-x64-v*.exe`
- **Linux**: `uvm-linux-x64-v*`  
- **macOS**: `uvm-osx-x64-v*`

下載後可直接執行，無需安裝 .NET 運行時。

### 從原始碼建置

如果您想要從原始碼建置，請參考下方的建置說明。

## 功能特色

- 🚀 自動檢查並更新到最新版本
- 📦 從 Google Drive 下載和安裝版本
- 🔄 版本間快速切換
- 🔒 SHA256 檔案完整性驗證
- 🔗 智慧型符號連結或目錄複製
- ⚙️ 支援 JSON 設定檔配置

## 設定檔配置

本專案使用 `appsettings.json` 進行設定管理，支援環境特定的設定檔：

### appsettings.json
```json
{
  "UpdateVersionManager": {
    "GoogleDriveVersionListFileId": "1HaA7rtbn_t7LWH67Pfr-tMV7cT7w7-E2",
    "LocalBaseDir": "app_versions",
    "CurrentVersionFile": "current_version.txt",
    "TempExtractPath": "temp_update",
    "ZipFilePath": "update.zip",
    "AppLinkName": "current"
  }
}
```

### 設定項目說明

- `GoogleDriveVersionListFileId`: Google Drive 上版本清單檔案的 ID
- `LocalBaseDir`: 本地版本儲存目錄
- `CurrentVersionFile`: 記錄當前版本的檔案名稱
- `TempExtractPath`: 臨時解壓縮目錄
- `ZipFilePath`: 下載的 ZIP 檔案路徑
- `AppLinkName`: 當前版本的連結目錄名稱

### 環境特定設定

您可以建立 `appsettings.Development.json` 用於開發環境，或 `appsettings.Production.json` 用於生產環境。環境變數 `DOTNET_ENVIRONMENT` 控制使用哪個設定檔。

## 命令使用

```bash
# 顯示幫助
uvm help

# 自動更新到最新版本
uvm update

# 列出已安裝版本
uvm list

# 列出遠端可用版本
uvm list-remote

# 安裝特定版本
uvm install <version>

# 切換到指定版本
uvm use <version>

# 顯示當前版本
uvm current

# 刪除指定版本
uvm clean <version>

# 計算檔案 SHA256
uvm hash <file>

# 產生版本資訊
uvm generate <version> <zip-file> <drive-file-id>

# 顯示連結資訊
uvm info
```

## 建置和執行

```bash
# 還原套件
dotnet restore

# 建置專案
dotnet build

# 執行應用程式
dotnet run -- <command>

# 發佈為單一執行檔（推薦）
dotnet publish src/UpdateVersionManager/UpdateVersionManager.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# 跨平台發佈
# Windows 64位元
dotnet publish src/UpdateVersionManager/UpdateVersionManager.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Linux 64位元
dotnet publish src/UpdateVersionManager/UpdateVersionManager.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true

# macOS 64位元
dotnet publish src/UpdateVersionManager/UpdateVersionManager.csproj -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true
```

### 執行測試

```bash
# 執行所有測試
dotnet test

# 執行測試並生成報告
dotnet test --configuration Release --logger trx --results-directory TestResults
```

## 技術架構

- **Framework**: .NET 9.0
- **設定管理**: Microsoft.Extensions.Configuration
- **相依性注入**: Microsoft.Extensions.DependencyInjection
- **檔案處理**: System.IO.Compression
- **JSON 處理**: System.Text.Json
- **HTTP 客戶端**: HttpClient

## 服務架構

```
Program.cs
├── ConfigurationBuilder (appsettings.json)
├── ServiceCollection (DI Container)
│   ├── UpdateVersionManagerSettings
│   ├── FileService
│   ├── GoogleDriveService
│   ├── SymbolicLinkService
│   └── VersionManager
└── CommandHandler
```

## Google Drive 設定

確保您的 Google Drive 檔案具有適當的分享權限：
1. 設定為「知道連結的任何人都能檢視」
2. 複製檔案 ID（URL 中的長字串）
3. 更新 `appsettings.json` 中的 `GoogleDriveVersionListFileId`

## 版本清單格式

版本清單 JSON 檔案格式：

```json
{
  "versions": [
    {
      "version": "1.0.0",
      "downloadUrl": "https://drive.google.com/uc?export=download&id=FILE_ID",
      "sha256": "abc123...",
      "size": 1024000,
      "releaseDate": "2025-01-01",
      "description": "Version 1.0.0"
    }
  ]
}
```

## 授權

本專案採用 MIT 授權。
