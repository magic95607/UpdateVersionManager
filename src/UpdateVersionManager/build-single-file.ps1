# 單一檔案發佈腳本 (PowerShell 版本)
# 生成跨平台的獨立可執行檔

Write-Host "🚀 開始建置單一檔案版本..." -ForegroundColor Green
Write-Host ""

# 清理之前的發佈
if (Test-Path "publish") {
    Remove-Item "publish" -Recurse -Force
}
New-Item -ItemType Directory -Path "publish" | Out-Null

# 定義目標平台
$targets = @(
    @{ Runtime = "win-x64"; Name = "Windows x64"; Output = "uvm.exe" },
    @{ Runtime = "linux-x64"; Name = "Linux x64"; Output = "uvm" },
    @{ Runtime = "osx-x64"; Name = "macOS x64"; Output = "uvm" },
    @{ Runtime = "osx-arm64"; Name = "macOS ARM64"; Output = "uvm" }
)

# 建置各平台版本
foreach ($target in $targets) {
    Write-Host "📦 建置 $($target.Name) 版本..." -ForegroundColor Yellow
    
    $outputPath = "publish\$($target.Runtime)"
    
    dotnet publish -c Release -r $target.Runtime --self-contained true -o $outputPath `
        /p:PublishSingleFile=true /p:PublishTrimmed=true | Out-Null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ $($target.Name) 建置成功" -ForegroundColor Green
    } else {
        Write-Host "   ❌ $($target.Name) 建置失敗" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "✅ 建置完成！發佈檔案位於:" -ForegroundColor Green

# 顯示檔案資訊
foreach ($target in $targets) {
    $filePath = "publish\$($target.Runtime)\$($target.Output)"
    if (Test-Path $filePath) {
        $fileInfo = Get-Item $filePath
        $sizeKB = [math]::Round($fileInfo.Length / 1KB, 2)
        $sizeMB = [math]::Round($fileInfo.Length / 1MB, 2)
        
        Write-Host "   📁 $filePath" -ForegroundColor Cyan
        Write-Host "      大小: $sizeKB KB ($sizeMB MB)" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "🎉 所有平台版本建置完成！" -ForegroundColor Green
Write-Host ""
Write-Host "💡 使用提示:" -ForegroundColor Yellow
Write-Host "   • Windows: .\publish\win-x64\uvm.exe help"
Write-Host "   • Linux:   ./publish/linux-x64/uvm help"
Write-Host "   • macOS:   ./publish/osx-x64/uvm help"
