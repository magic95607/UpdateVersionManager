@echo off
REM 單一檔案發佈腳本
REM 生成跨平台的獨立可執行檔

echo 🚀 開始建置單一檔案版本...
echo.

REM 清理之前的發佈
if exist "publish" rd /s /q "publish"
mkdir "publish"

echo 📦 建置 Windows x64 版本...
dotnet publish -c Release -r win-x64 --self-contained true -o "publish\win-x64" -p:PublishSingleFile=true -p:PublishTrimmed=true

echo 📦 建置 Linux x64 版本...
dotnet publish -c Release -r linux-x64 --self-contained true -o "publish\linux-x64" -p:PublishSingleFile=true -p:PublishTrimmed=true

echo 📦 建置 macOS x64 版本...
dotnet publish -c Release -r osx-x64 --self-contained true -o "publish\osx-x64" -p:PublishSingleFile=true -p:PublishTrimmed=true

echo 📦 建置 macOS ARM64 版本...
dotnet publish -c Release -r osx-arm64 --self-contained true -o "publish\osx-arm64" -p:PublishSingleFile=true -p:PublishTrimmed=true

echo.
echo ✅ 建置完成！發佈檔案位於:
echo   📁 publish\win-x64\uvm.exe          (Windows x64)
echo   📁 publish\linux-x64\uvm            (Linux x64)
echo   📁 publish\osx-x64\uvm              (macOS x64)
echo   📁 publish\osx-arm64\uvm            (macOS ARM64)
echo.

REM 顯示檔案大小
echo 📊 檔案大小:
for %%f in ("publish\win-x64\uvm.exe") do echo   Windows: %%~zf bytes
for %%f in ("publish\linux-x64\uvm") do echo   Linux:   %%~zf bytes
for %%f in ("publish\osx-x64\uvm") do echo   macOS:    %%~zf bytes
for %%f in ("publish\osx-arm64\uvm") do echo   macOS ARM: %%~zf bytes

echo.
echo 🎉 所有平台版本建置完成！
pause
