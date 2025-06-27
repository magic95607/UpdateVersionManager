# GitHub Release 發布指南

本文檔說明如何將 UpdateVersionManager 發布到 GitHub Releases。

## 🎯 發布選項

### 方法一：自動化 GitHub Actions（推薦）

#### 使用現有 Tag 觸發
您已經有 tag `1.0.0`，可以直接推送來觸發自動建置：

```bash
# 推送 tag 到 GitHub（如果還沒推送）
git push origin 1.0.0

# 或重新建立並推送 tag
git tag -d 1.0.0
git tag -a 1.0.0 -m "Release version 1.0.0"  
git push origin 1.0.0
```

#### 手動觸發工作流程
在 GitHub 網頁介面：
1. 前往 `Actions` 頁籤
2. 選擇 `Release` 工作流程
3. 點擊 `Run workflow`
4. 輸入版本號（如 `1.0.0`）
5. 點擊綠色的 `Run workflow` 按鈕

### 方法二：使用發布腳本

#### Windows 使用者
```cmd
# 給予執行權限並執行
release.bat 1.0.0
```

#### Linux/macOS 使用者
```bash
# 給予執行權限
chmod +x release.sh

# 執行發布腳本
./release.sh 1.0.0
```

### 方法三：手動建置和發布

#### 1. 建置各平台版本

```bash
# Windows x64
dotnet publish src/UpdateVersionManager/UpdateVersionManager.csproj \
  -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:Version=1.0.0 \
  -o publish/win-x64

# Linux x64  
dotnet publish src/UpdateVersionManager/UpdateVersionManager.csproj \
  -c Release -r linux-x64 --self-contained true \
  -p:PublishSingleFile=true -p:Version=1.0.0 \
  -o publish/linux-x64

# macOS x64
dotnet publish src/UpdateVersionManager/UpdateVersionManager.csproj \
  -c Release -r osx-x64 --self-contained true \
  -p:PublishSingleFile=true -p:Version=1.0.0 \
  -o publish/osx-x64
```

#### 2. 重新命名檔案

```bash
# Windows
mv publish/win-x64/uvm.exe uvm-win-x64-v1.0.0.exe

# Linux
mv publish/linux-x64/uvm uvm-linux-x64-v1.0.0
chmod +x uvm-linux-x64-v1.0.0

# macOS  
mv publish/osx-x64/uvm uvm-osx-x64-v1.0.0
chmod +x uvm-osx-x64-v1.0.0
```

#### 3. 計算 SHA256

```bash
# Windows (PowerShell)
Get-FileHash uvm-win-x64-v1.0.0.exe -Algorithm SHA256 > uvm-win-x64-v1.0.0.exe.sha256

# Linux/macOS
sha256sum uvm-linux-x64-v1.0.0 > uvm-linux-x64-v1.0.0.sha256
sha256sum uvm-osx-x64-v1.0.0 > uvm-osx-x64-v1.0.0.sha256
```

#### 4. 使用 GitHub CLI 建立 Release

```bash
# 安裝 GitHub CLI (如果尚未安裝)
# Windows: winget install GitHub.cli
# macOS: brew install gh
# Linux: 參考 https://cli.github.com/

# 登入 GitHub
gh auth login

# 建立 Release
gh release create 1.0.0 \
  uvm-win-x64-v1.0.0.exe \
  uvm-win-x64-v1.0.0.exe.sha256 \
  uvm-linux-x64-v1.0.0 \
  uvm-linux-x64-v1.0.0.sha256 \
  uvm-osx-x64-v1.0.0 \
  uvm-osx-x64-v1.0.0.sha256 \
  --title "UpdateVersionManager v1.0.0" \
  --notes-file CHANGELOG.md
```

## 🔄 GitHub Actions 工作流程說明

我們建立的 `.github/workflows/release.yml` 包含以下功能：

### 觸發條件
- **Tag Push**: 當推送符合 `v*.*.*` 或 `*.*.*` 格式的 tag 時
- **手動觸發**: 在 GitHub Actions 頁面手動執行

### 建置流程
1. **測試階段**: 在 Ubuntu 上執行所有單元測試
2. **建置階段**: 在 Windows、Linux、macOS 上並行建置
   - 建立單一檔案執行檔
   - 計算 SHA256 雜湊值
   - 上傳為 artifacts
3. **發布階段**: 建立 GitHub Release
   - 下載所有 artifacts
   - 生成詳細的 Release Notes
   - 上傳所有檔案到 Release

### 產出檔案
每個 Release 包含：
- `uvm-win-x64-v{version}.exe` - Windows 執行檔
- `uvm-linux-x64-v{version}` - Linux 執行檔  
- `uvm-osx-x64-v{version}` - macOS 執行檔
- 對應的 `.sha256` 檔案用於驗證

## 📋 發布前檢查清單

### ✅ 必要檢查
- [ ] 所有測試通過 (`dotnet test`)
- [ ] 版本號已更新在 `UpdateVersionManager.csproj`
- [ ] `CHANGELOG.md` 已更新
- [ ] Git 工作目錄乾淨（無未提交變更）
- [ ] 已建立並推送 tag

### ✅ 可選檢查  
- [ ] 文檔已更新
- [ ] 範例程式碼已測試
- [ ] 手動測試單一檔案版本
- [ ] 檢查不同平台的相容性

## 🎛️ Release 設定

### 版本號規則
使用語意化版本控制：
- `1.0.0` - 主要版本
- `1.1.0` - 次要版本（新功能）
- `1.0.1` - 修補版本（錯誤修復）

### Tag 格式
支援兩種格式：
- `1.0.0` - 直接版本號
- `v1.0.0` - 帶 v 前綴

### Release 類型
- **正式版本**: 穩定功能，生產環境就緒
- **預發布版本**: 添加 `-beta`, `-alpha` 後綴
- **草稿版本**: 設定 `draft: true`

## 🔍 驗證發布

### 檢查清單
1. **GitHub Release 頁面**: 確認所有檔案已上傳
2. **下載測試**: 下載並測試每個平台的執行檔
3. **SHA256 驗證**: 確認檔案完整性
4. **功能測試**: 執行基本命令確認正常運作

### 測試命令
```bash
# 基本功能測試
./uvm-{platform}-v{version} help
./uvm-{platform}-v{version} --version
./uvm-{platform}-v{version} check

# 自更新測試（謹慎使用）
./uvm-{platform}-v{version} self-update
```

## 🐛 常見問題

### Q: GitHub Actions 建置失敗
**A**: 檢查以下項目：
- 確認所有測試通過
- 檢查 `UpdateVersionManager.csproj` 語法
- 查看 Actions 日誌詳細錯誤訊息

### Q: 檔案大小異常
**A**: 單一檔案約 84MB 屬正常，包含完整 .NET 運行時

### Q: 某平台執行檔無法運行
**A**: 確認：
- 檔案有執行權限（Linux/macOS）
- 目標系統相容性
- 防毒軟體是否攔截

### Q: Release Notes 沒有正確顯示
**A**: 確認 `CHANGELOG.md` 格式正確，並且推送到 main 分支

## 📚 相關文檔

- [GitHub Actions 文檔](https://docs.github.com/en/actions)
- [GitHub CLI 文檔](https://cli.github.com/manual/)
- [.NET 單一檔案發佈](https://docs.microsoft.com/en-us/dotnet/core/deploying/single-file)
- [語意化版本控制](https://semver.org/lang/zh-TW/)

---

## 🚀 快速開始

如果您已經有 tag `1.0.0`，最簡單的方式是：

```bash
# 推送現有 tag 觸發自動建置
git push origin 1.0.0
```

然後前往 GitHub 的 Actions 頁面查看建置進度！
