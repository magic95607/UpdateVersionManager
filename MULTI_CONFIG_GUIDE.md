# UVM 多設定檔與多應用管理使用指南

## 概述

UVM (UpdateVersionManager) 現在支援多設定檔自動偵測和多應用管理，讓您可以將 UVM 設為環境變數，在不同的專案目錄下自動使用對應的設定檔管理不同的應用版本。

## 新功能特點

### 1. 多下載來源支援
- **Google Drive**: `https://drive.google.com/...`
- **GitHub**: `https://github.com/...` 
- **HTTP/HTTPS**: `https://...`
- **FTP**: `ftp://...`
- **SFTP**: `sftp://...`

### 2. 多設定檔自動偵測
UVM 會按照以下順序搜尋並載入設定檔：

1. **命令行參數指定**: `--config <path>` 或 `-c <path>`
2. **環境變數**: `UVM_CONFIG` 指定的路徑
3. **當前目錄**: 依序搜尋 `uvm.json`, `appsettings.json`, `versions.json`
4. **預設設定**: 應用程式內建的 `appsettings.json`

### 3. 智慧相對路徑解析
- 當設定檔中使用相對路徑時，會優先相對於設定檔所在目錄解析
- 如果找不到，則相對於當前工作目錄解析

## 使用場景

### 場景 1: 管理多個 Node.js 版本 (NVM 風格)

1. **建立 NVM 目錄和設定檔**:
   ```bash
   mkdir nvm
   cd nvm
   ```

2. **創建 `uvm.json`**:
   ```json
   {
     "UpdateVersionManager": {
       "VersionListSource": "./versions.json",
       "LocalBaseDir": "node_versions",
       "CurrentVersionFile": "current_node_version.txt",
       "TempExtractPath": "temp_node_update",
       "ZipFilePath": "node_update.zip",
       "AppLinkName": "current",
       "VerboseOutput": true
     }
   }
   ```

3. **創建 `versions.json`**:
   ```json
   {
     "versions": [
       {
         "version": "18.19.0",
         "downloadUrl": "https://nodejs.org/dist/v18.19.0/node-v18.19.0-win-x64.zip",
         "sha256": "...",
         "size": 15728640,
         "releaseDate": "2024-01-15",
         "description": "Node.js 18.19.0 LTS"
       }
     ]
   }
   ```

4. **使用**:
   ```bash
   # 在 nvm 目錄下執行，自動使用當前目錄的設定檔
   uvm list-remote           # 列出可用的 Node.js 版本
   uvm install 18.19.0       # 安裝指定版本
   uvm use 18.19.0           # 切換到指定版本
   ```

### 場景 2: 管理自定義應用

1. **建立應用目錄和設定檔**:
   ```bash
   mkdir myapp
   cd myapp
   ```

2. **創建 `uvm.json`** (使用 Google Drive):
   ```json
   {
     "UpdateVersionManager": {
       "GoogleDriveVersionListFileId": "1HaA7rtbn_t7LWH67Pfr-tMV7cT7w7-E2",
       "LocalBaseDir": "myapp_versions",
       "CurrentVersionFile": "current_myapp_version.txt",
       "TempExtractPath": "temp_myapp_update",
       "ZipFilePath": "myapp_update.zip",
       "AppLinkName": "current",
       "VerboseOutput": false
     }
   }
   ```

3. **使用**:
   ```bash
   # 在 myapp 目錄下執行，自動使用 Google Drive 設定
   uvm list-remote           # 列出 Google Drive 上的版本
   uvm install latest        # 安裝最新版本
   ```

## 命令使用方式

### 基本命令
```bash
# 自動偵測當前目錄設定檔
uvm list-remote
uvm install <version>
uvm use <version>
uvm current
uvm list

# 指定設定檔
uvm --config ./custom.json list-remote
uvm -c ./custom.json install latest

# 使用環境變數
export UVM_CONFIG=./myapp.json  # Linux/macOS
$env:UVM_CONFIG="./myapp.json"  # Windows PowerShell
uvm list-remote
```

### 進階使用
```bash
# 管理版本
uvm clean <version>           # 刪除指定版本
uvm hash <file>               # 計算檔案 SHA256
uvm generate <ver> <zip> <id> # 產生版本資訊

# 查看資訊
uvm check                     # 顯示當前連結資訊
uvm help                      # 顯示幫助資訊
```

## 環境變數設定

### 將 UVM 設為環境變數

**Windows (PowerShell)**:
```powershell
# 將 UVM 加到 PATH
$env:PATH += ";d:\path\to\uvm"

# 或將 uvm.exe 複製到已在 PATH 中的目錄
```

**Linux/macOS**:
```bash
# 加到 ~/.bashrc 或 ~/.zshrc
export PATH="/path/to/uvm:$PATH"

# 或建立符號連結
sudo ln -s /path/to/uvm/uvm /usr/local/bin/uvm
```

### 設定檔環境變數
```bash
# 全域設定檔
export UVM_CONFIG="/path/to/global/config.json"

# 專案特定設定檔
cd /path/to/project
export UVM_CONFIG="./project-config.json"
```

## 設定檔格式

### 完整設定檔範例
```json
{
  "UpdateVersionManager": {
    "VersionListSource": "./versions.json",     // 本地版本清單檔案
    "VersionListUrl": "https://api.example.com/versions.json", // 遠端版本清單 URL  
    "GoogleDriveVersionListFileId": "1HaA...",  // Google Drive 檔案 ID
    "LocalBaseDir": "app_versions",             // 版本安裝目錄
    "CurrentVersionFile": "current_version.txt", // 當前版本記錄檔
    "TempExtractPath": "temp_update",           // 臨時解壓目錄
    "ZipFilePath": "update.zip",                // 下載檔案名稱
    "AppLinkName": "current",                   // 符號連結名稱
    "VerboseOutput": false                      // 詳細輸出
  },
  "Serilog": {
    // 日誌設定...
  }
}
```

### 版本清單檔案格式
```json
{
  "versions": [
    {
      "version": "1.2.0",
      "downloadUrl": "https://example.com/app-1.2.0.zip",
      "sha256": "abc123...",
      "size": 1048576,
      "releaseDate": "2024-01-15", 
      "description": "Version 1.2.0 with new features"
    }
  ]
}
```

## 工作流程範例

### 典型的多應用管理工作流程

1. **設定環境**:
   ```bash
   # 將 uvm 加到 PATH
   export PATH="/path/to/uvm:$PATH"
   ```

2. **建立專案結構**:
   ```
   projects/
   ├── nvm/                 # Node.js 版本管理
   │   ├── uvm.json
   │   ├── versions.json
   │   └── node_versions/
   ├── python-manager/      # Python 版本管理  
   │   ├── uvm.json
   │   ├── versions.json
   │   └── python_versions/
   └── myapp/              # 自定義應用
       ├── uvm.json
       └── myapp_versions/
   ```

3. **使用**:
   ```bash
   # 管理 Node.js
   cd projects/nvm
   uvm install 18.19.0
   uvm use 18.19.0
   
   # 管理 Python
   cd ../python-manager  
   uvm list-remote
   uvm install 3.11.0
   
   # 管理自定義應用
   cd ../myapp
   uvm update
   ```

## 故障排除

### 常見問題

1. **找不到設定檔**:
   - 檢查當前目錄是否有 `uvm.json`、`appsettings.json` 或 `versions.json`
   - 確認 `UVM_CONFIG` 環境變數路徑正確
   - 使用 `--config` 參數指定設定檔路徑

2. **相對路徑無法解析**:
   - 確認相對路徑相對於設定檔所在目錄是否正確
   - 使用絕對路徑避免路徑解析問題

3. **下載失敗**:
   - 檢查網路連線
   - 確認下載 URL 格式正確
   - 檢查 Google Drive 分享設定（如適用）

### 除錯技巧

1. **啟用詳細輸出**:
   ```json
   {
     "UpdateVersionManager": {
       "VerboseOutput": true
     }
   }
   ```

2. **檢查設定檔載入**:
   ```bash
   uvm help  # 會顯示設定檔搜尋順序
   ```

3. **查看日誌**:
   ```bash
   # 日誌檔案位置通常在 logs/ 目錄下
   tail -f logs/uvm-*.log
   ```

## 最佳實踐

1. **設定檔命名**: 建議使用 `uvm.json` 作為專案特定的設定檔名稱
2. **版本管理**: 使用語意化版本號 (SemVer)
3. **目錄結構**: 為不同應用建立獨立的目錄和設定檔
4. **備份**: 定期備份重要的設定檔和版本清單
5. **安全**: 確保下載來源的安全性，驗證檔案 SHA256

---

透過這些新功能，UVM 現在可以靈活地管理多個應用的版本，並且支援多種下載來源，讓版本管理變得更加簡單和高效。
