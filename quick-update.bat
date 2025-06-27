@echo off
REM 快速自檢並更新批次檔案 - 供主程序調用
REM 用法: quick-update.bat [--clean] [--silent]

setlocal EnableDelayedExpansion

REM 設定編碼為 UTF-8
chcp 65001 >nul

REM 切換到 UpdateVersionManager 目錄
cd /d "%~dp0src\UpdateVersionManager"

REM 檢查是否有 --silent 參數
set SILENT=0
for %%i in (%*) do (
    if "%%i"=="--silent" set SILENT=1
)

REM 執行快速更新
if !SILENT!==1 (
    dotnet run -- self-update %* >nul 2>&1
    if !ERRORLEVEL!==0 (
        echo SUCCESS
    ) else (
        echo FAILED
    )
) else (
    dotnet run -- self-update %*
)

endlocal
