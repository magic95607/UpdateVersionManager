#!/bin/bash
# 快速自檢並更新腳本 - 供主程序調用 (Linux/macOS 版本)
# 用法: ./quick-update.sh [--clean] [--silent]

set -e

# 檢查是否有 --silent 參數
SILENT=false
for arg in "$@"; do
    if [ "$arg" = "--silent" ]; then
        SILENT=true
        break
    fi
done

# 切換到 UpdateVersionManager 目錄
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR/src/UpdateVersionManager"

# 執行快速更新
if [ "$SILENT" = true ]; then
    if dotnet run -- self-update "$@" >/dev/null 2>&1; then
        echo "SUCCESS"
    else
        echo "FAILED"
    fi
else
    dotnet run -- self-update "$@"
fi
