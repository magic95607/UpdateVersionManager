# 快速自檢並更新 PowerShell 腳本 - 供主程序調用
# 用法: .\quick-update.ps1 [-Clean] [-Silent]

param(
    [switch]$Clean,
    [switch]$Silent
)

# 設定編碼
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
[Console]::InputEncoding = [System.Text.Encoding]::UTF8

# 切換到 UpdateVersionManager 目錄
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location (Join-Path $ScriptDir "src\UpdateVersionManager")

# 準備參數
$Args = @("self-update")
if ($Clean) { $Args += "--clean" }

try {
    if ($Silent) {
        $Output = & dotnet run -- @Args 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Output "SUCCESS"
        } else {
            Write-Output "FAILED"
            Write-Error $Output
        }
    } else {
        & dotnet run -- @Args
    }
} catch {
    if ($Silent) {
        Write-Output "FAILED"
    } else {
        Write-Error "執行失敗: $_"
    }
}
