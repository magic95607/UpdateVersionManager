# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-06-27

### 🎉 Initial Release

#### Added
- **現代化架構重構**
  - 從硬編碼設定遷移到 `appsettings.json` 配置檔案
  - 支援開發/生產環境不同設定（`appsettings.Development.json`）
  - 整合 Microsoft.Extensions.Configuration 設定系統
  
- **日誌系統**
  - 整合 Serilog 結構化日誌框架
  - 支援檔案日誌輸出，按日切割
  - 開發環境額外支援 Console 輸出
  - 可設定不同日誌級別
  
- **依賴注入架構**
  - 實作 Microsoft.Extensions.DependencyInjection 容器
  - 服務生命週期管理
  - 介面與實作分離，提升可測試性
  
- **輸出服務重構**
  - 新增 `IOutputService` 介面分離 Console 和 Log 輸出
  - 支援測試環境輸出捕獲
  - 統一的輸出管理機制
  
- **完整測試覆蓋**
  - 49 個單元測試，100% 通過率
  - 使用 xUnit、FluentAssertions、Moq 測試框架
  - 建立 `TestBase` 基礎類別統一測試環境
  - 涵蓋所有核心功能的測試案例
  
- **自我更新功能**
  - 新增 `self-update` 命令支援程式自我更新
  - 新增 `auto` 命令供腳本自動化使用
  - 新增 `quick-update` 命令快速更新
  - 支援 `--clean` 參數自動清理舊版本
  - 提供 `QuickUpdater` 靜態類別供主程式整合
  
- **跨平台腳本支援**
  - `quick-update.bat` - Windows 批次腳本
  - `quick-update.ps1` - PowerShell 腳本
  - `quick-update.sh` - Linux/macOS Bash 腳本
  
- **單一檔案發佈**
  - 支援 .NET Single File 發佈
  - 自包含運行時，無需額外安裝 .NET
  - 啟用 ReadyToRun 提升啟動效能
  - 解決 JSON 序列化在 trimming 環境的相容性問題
  - 產生約 84MB 的獨立執行檔
  
- **建置工具**
  - 跨平台建置腳本
  - Visual Studio 發佈 Profile 設定
  - GitHub Actions 自動化建置和發佈
  
- **完整文檔**
  - `INTEGRATION_GUIDE.md` - 主程式整合指南
  - `SINGLE_FILE_GUIDE.md` - 單一檔案發佈指南  
  - `SELF_UPDATE_COMPLETED.md` - 自我更新功能說明
  - `PROJECT_COMPLETION_SUMMARY.md` - 專案完成總結
  - `Examples/MainProgramIntegration.cs` - 整合範例程式碼

#### Technical Details
- **目標框架**: .NET 9.0
- **支援平台**: Windows (win-x64), Linux (linux-x64), macOS (osx-x64)
- **主要依賴**: 
  - Microsoft.Extensions.Configuration 9.0.0
  - Microsoft.Extensions.DependencyInjection 9.0.0
  - Serilog 4.1.0
- **測試框架**:
  - xUnit 2.8.2
  - FluentAssertions 6.12.2
  - Moq 4.20.72

#### Breaking Changes
- 設定檔從硬編碼改為外部 JSON 檔案
- 命令列參數和輸出格式可能有細微變化
- 需要 .NET 9.0 運行時（單一檔案版本除外）

#### Migration Guide
從舊版本升級時：
1. 建立 `appsettings.json` 設定檔案
2. 設定 Google Drive 檔案 ID 和本地路徑
3. 更新整合程式碼使用新的 `QuickUpdater` 類別

## [Unreleased]

### Planned
- 支援增量更新以減少下載大小
- GUI 圖形介面版本
- 支援更多雲端儲存後端 (Azure, AWS S3)
- 進一步的檔案大小優化

---

## Version Tags
- `1.0.0` - 2025-06-27: 首次正式發布，包含完整重構功能
