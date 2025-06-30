# UpdateVersionManager (UVM)

通用版本管理工具，支援多來源下載和多設定檔自動偵測，可管理不同應用的版本。

## 🎯 新功能亮點

### 🔧 多設定檔自動偵測
- **自動偵測**: 根據當前工作目錄自動載入對應的設定檔
- **多應用管理**: 在不同目錄下管理不同應用的版本（如 Node.js、Python、自定義應用）
- **靈活配置**: 支援命令行參數、環境變數、當前目錄設定檔等多種配置方式

### 🌐 SFTP 支援
- 新增 **SFTP** 下載支援，適用於安全的檔案傳輸需求
- 完整支援 SSH 金鑰認證和密碼認證

## 快速開始

### 下載預編譯版本

從 [GitHub Releases](https://github.com/magic95607/UpdateVersionManager/releases/latest) 下載對應您作業系統的預編譯版本：

- **Windows**: `uvm-win-x64-v*.exe`
- **Linux**: `uvm-linux-x64-v*`  
- **macOS**: `uvm-osx-x64-v*`

下載後可直接執行，無需安裝 .NET 運行時。

### 設為環境變數（建議）

將 UVM 加入系統 PATH，即可在任意目錄使用：

**Windows**:
```powershell
# 將 uvm.exe 所在目錄加到 PATH
$env:PATH += ";C:\path\to\uvm"
```

**Linux/macOS**:
```bash
# 加到 ~/.bashrc 或 ~/.zshrc
export PATH="/path/to/uvm:$PATH"
```

### 從原始碼建置

如果您想要從原始碼建置，請參考下方的建置說明。

## 功能特色

- 🚀 自動檢查並更新到最新版本
- 📦 多來源下載支援（Google Drive、GitHub、HTTP、FTP、SFTP）
- 🔄 版本間快速切換
- 🔒 SHA256 檔案完整性驗證
- 🔗 智慧型符號連結或目錄複製
- ⚙️ 多設定檔自動偵測
- 🌐 自動偵測 URL 來源類型
- 📂 多應用版本管理

## 多設定檔使用方式

### 設定檔搜尋順序
1. **命令行參數**: `--config <path>` 或 `-c <path>`
2. **環境變數**: `UVM_CONFIG` 指定的路徑
3. **當前目錄**: 依序搜尋 `uvm.json`, `appsettings.json`, `versions.json`
4. **預設設定**: 應用程式內建的 `appsettings.json`

### 使用範例

**管理 Node.js 版本 (NVM 風格)**:
```bash
# 建立 nvm 目錄
mkdir nvm && cd nvm

# 創建 uvm.json 和 versions.json
# ... (設定檔內容請參考 MULTI_CONFIG_GUIDE.md)

# 使用 (自動偵測當前目錄設定檔)
uvm list-remote    # 列出可用的 Node.js 版本  
uvm install 18.19.0
uvm use 18.19.0
```

**管理自定義應用**:
```bash
# 建立應用目錄  
mkdir myapp && cd myapp

# 創建 uvm.json (使用 Google Drive 或其他來源)
# ...

# 使用
uvm install latest
uvm update
```

**跨目錄使用指定設定檔**:
```bash
# 使用命令行參數指定設定檔
uvm --config ./nvm/uvm.json list-remote

# 使用環境變數
export UVM_CONFIG="./myapp/uvm.json"  
uvm install latest
```

## 多來源下載支援

本工具支援從多種來源下載版本檔案：

### 支援的來源類型

1. **Google Drive** - 適用於私人或小團隊使用
   - URL 格式：`https://drive.google.com/file/d/{fileId}/view`
   - 自動處理病毒掃描確認頁面
   - 支援大檔案下載

2. **GitHub Release** - 適用於開源專案
   - URL 格式：`https://github.com/user/repo/releases/download/v1.0.0/file.zip`
   - 或原始內容：`https://raw.githubusercontent.com/user/repo/main/file.txt`

3. **SFTP** - 安全檔案傳輸協定 🆕
   - URL 格式：`sftp://username:password@hostname:port/path/file.zip`
   - 支援 SSH 金鑰認證

4. **FTP/FTPS** - 適用於企業內部部署
   - URL 格式：`ftp://example.com/path/file.zip`
   - 支援匿名和認證登入

5. **HTTP/HTTPS** - 通用網路資源
   - URL 格式：`https://example.com/file.zip`

### URL 自動偵測

工具會根據 URL 格式自動選擇合適的下載方式：
- 包含 `drive.google.com` 或 `docs.google.com` → Google Drive 下載器
- 包含 `github.com` 或 `githubusercontent.com` → GitHub 下載器  
- 協議為 `ftp://` 或 `ftps://` → FTP 下載器
- 其他 → 標準 HTTP 下載器

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

- `GoogleDriveVersionListFileId`: Google Drive 上版本清單檔案的 ID（也可以是完整的下載 URL）
- `LocalBaseDir`: 本地版本儲存目錄
- `CurrentVersionFile`: 記錄當前版本的檔案名稱
- `TempExtractPath`: 臨時解壓縮目錄
- `ZipFilePath`: 下載的 ZIP 檔案路徑
- `AppLinkName`: 當前版本的連結目錄名稱

### 版本清單 URL 設定

版本清單可以來自多種來源：

```json
{
  "UpdateVersionManager": {
    // Google Drive 檔案 ID
    "VersionListUrl": "1HaA7rtbn_t7LWH67Pfr-tMV7cT7w7-E2",
    
    // 或完整的 Google Drive 下載 URL
    "VersionListUrl": "https://drive.google.com/uc?export=download&id=1HaA7rtbn_t7LWH67Pfr-tMV7cT7w7-E2",
    
    // 或 GitHub 原始檔案 URL
    "VersionListUrl": "https://raw.githubusercontent.com/user/repo/main/versions.json",
    
    // 或 FTP URL
    "VersionListUrl": "ftp://ftp.example.com/path/versions.json",
    
    // 或任何 HTTP(S) URL
    "VersionListUrl": "https://example.com/api/versions.json"
  }
}
```

### 環境特定設定

您可以建立 `appsettings.Development.json` 用於開發環境，或 `appsettings.Production.json` 用於生產環境。環境變數 `DOTNET_ENVIRONMENT` 控制使用哪個設定檔。

## 命令使用

```bash
# 顯示幫助
uvm help

# 自動更新到最新版本
uvm update

# 快速自檢並更新（適合腳本使用）
uvm self-update
uvm auto

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

### 自動更新功能

UpdateVersionManager 支援自我更新功能：

- `uvm update`：互動式更新，會顯示詳細進度
- `uvm self-update` 或 `uvm auto`：靜默更新，適合在腳本中使用
- `uvm quick-update`：快速更新模式

所有更新命令都會：
1. 檢查遠端是否有新版本
2. 下載並驗證檔案完整性（SHA256）
3. 自動替換當前執行檔
4. 可選擇清理舊版本（使用 `--clean` 參數）

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

### 避免病毒掃描問題

對於較大的檔案（通常 > 100MB），Google Drive 可能會顯示病毒掃描警告頁面。建議：

1. **分割大檔案**：將大於 100MB 的檔案分割成較小的部分
2. **使用壓縮**：確保 ZIP 檔案儘可能小
3. **直接下載連結**：在 Google Drive 中右鍵選擇「取得連結」，確保設定為公開存取

### 疑難排解

如果遇到「Google Drive 病毒掃描頁面：無法找到確認下載連結」錯誤：

1. 檢查檔案是否設定為公開存取
2. 嘗試手動從瀏覽器下載該檔案，確認可以正常下載
3. 考慮使用其他雲端儲存服務（如 GitHub Releases）
4. 聯絡管理員檢查 Google Drive 檔案設定

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

## 已知問題與限制

### Google Drive 相關
- **病毒掃描警告**：大檔案（>100MB）可能觸發 Google Drive 的病毒掃描機制，導致下載失敗
- **下載限制**：頻繁下載可能觸發 Google Drive 的速率限制
- **檔案權限**：檔案必須設定為「知道連結的任何人都能檢視」

### 平台相關
- **符號連結**：Windows 環境下某些情況可能需要管理員權限
- **檔案鎖定**：正在使用的應用程式檔案無法被替換，需要先關閉應用程式

### 建議解決方案
- 使用 GitHub Releases 替代 Google Drive 來託管版本檔案
- 保持檔案大小在合理範圍內（建議 < 50MB）
- 定期檢查雲端儲存的檔案可用性

## 授權

本專案採用 MIT 授權。
