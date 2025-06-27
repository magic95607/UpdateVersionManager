@echo off
setlocal enabledelayedexpansion

REM GitHub Release 發布腳本 (Windows)
REM 使用方式: release.bat 1.0.0

if "%~1"=="" (
    echo [ERROR] 請提供版本號
    echo 使用方式: %0 ^<version^>
    echo 範例: %0 1.0.0
    exit /b 1
)

set VERSION=%1
set TAG_NAME=v%VERSION%

echo [INFO] 準備發布 UpdateVersionManager %VERSION%

REM 檢查是否在 git 倉庫中
git rev-parse --git-dir >nul 2>&1
if errorlevel 1 (
    echo [ERROR] 當前目錄不是 Git 倉庫
    exit /b 1
)

REM 檢查是否有未提交的變更
git diff-index --quiet HEAD -- >nul 2>&1
if errorlevel 1 (
    echo [WARNING] 發現未提交的變更
    set /p continue="是否要繼續? (y/N): "
    if /i not "!continue!"=="y" (
        echo [INFO] 發布已取消
        exit /b 1
    )
)

REM 檢查 tag 是否已存在
git rev-parse %TAG_NAME% >nul 2>&1
if not errorlevel 1 (
    echo [WARNING] Tag %TAG_NAME% 已存在
    set /p recreate="是否要刪除並重新建立? (y/N): "
    if /i "!recreate!"=="y" (
        echo [INFO] 刪除現有 tag...
        git tag -d %TAG_NAME% 2>nul
        git push origin --delete %TAG_NAME% 2>nul
    ) else (
        echo [INFO] 發布已取消
        exit /b 1
    )
)

REM 執行測試
echo [INFO] 執行測試...
dotnet test test/UpdateVersionManager.Tests/UpdateVersionManager.Tests.csproj --configuration Release
if errorlevel 1 (
    echo [ERROR] 測試失敗
    exit /b 1
)
echo [SUCCESS] 所有測試通過

REM 建置各平台版本
echo [INFO] 建置發布版本...

if not exist "release\v%VERSION%" mkdir "release\v%VERSION%"

REM Windows x64
echo [INFO] 建置 win-x64 版本...
dotnet publish src/UpdateVersionManager/UpdateVersionManager.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:PublishTrimmed=false ^
    -p:Version=%VERSION% ^
    -p:AssemblyVersion=%VERSION%.0 ^
    -p:FileVersion=%VERSION%.0 ^
    -o "release\v%VERSION%\win-x64"

move "release\v%VERSION%\win-x64\uvm.exe" "release\v%VERSION%\uvm-win-x64-v%VERSION%.exe"
rmdir /s /q "release\v%VERSION%\win-x64"

REM 計算 SHA256 (Windows)
echo [INFO] 計算 win-x64 版本 SHA256...
cd "release\v%VERSION%"
certutil -hashfile "uvm-win-x64-v%VERSION%.exe" SHA256 | find /v ":" | find /v "CertUtil" > "uvm-win-x64-v%VERSION%.exe.sha256"
cd ..\..

echo [SUCCESS] win-x64 版本建置完成

REM 顯示檔案
echo [INFO] 發布檔案:
dir "release\v%VERSION%\"

REM 建立並推送 tag
echo [INFO] 建立 Git tag...
git add .
git commit -m "chore: bump version to %VERSION%" 2>nul || echo [WARNING] 沒有變更需要提交
git tag -a %TAG_NAME% -m "Release version %VERSION%"

echo [INFO] 推送到遠端倉庫...
git push origin main
git push origin %TAG_NAME%

echo [SUCCESS] Tag %TAG_NAME% 已建立並推送

echo [INFO] GitHub Actions 將自動建立 Release
echo [INFO] 請前往 GitHub 查看發布進度

echo [SUCCESS] 發布流程完成！

pause
