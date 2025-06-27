#!/bin/bash

# GitHub Release 發布腳本
# 使用方式: ./release.sh 1.0.0

set -e

# 顏色定義
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 函數：印出彩色訊息
print_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# 檢查參數
if [ $# -eq 0 ]; then
    print_error "請提供版本號"
    echo "使用方式: $0 <version>"
    echo "範例: $0 1.0.0"
    exit 1
fi

VERSION="$1"
TAG_NAME="v$VERSION"

print_info "準備發布 UpdateVersionManager $VERSION"

# 檢查是否在 git 倉庫中
if ! git rev-parse --git-dir > /dev/null 2>&1; then
    print_error "當前目錄不是 Git 倉庫"
    exit 1
fi

# 檢查是否有未提交的變更
if ! git diff-index --quiet HEAD --; then
    print_warning "發現未提交的變更"
    read -p "是否要繼續? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        print_info "發布已取消"
        exit 1
    fi
fi

# 檢查 tag 是否已存在
if git rev-parse "$TAG_NAME" >/dev/null 2>&1; then
    print_warning "Tag $TAG_NAME 已存在"
    read -p "是否要刪除並重新建立? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        print_info "刪除現有 tag..."
        git tag -d "$TAG_NAME" || true
        git push origin --delete "$TAG_NAME" || true
    else
        print_info "發布已取消"
        exit 1
    fi
fi

# 更新版本號在專案檔中
print_info "更新專案檔版本號..."
sed -i.bak "s/<Version>.*<\/Version>/<Version>$VERSION<\/Version>/" src/UpdateVersionManager/UpdateVersionManager.csproj
sed -i.bak "s/<AssemblyVersion>.*<\/AssemblyVersion>/<AssemblyVersion>$VERSION.0<\/AssemblyVersion>/" src/UpdateVersionManager/UpdateVersionManager.csproj
sed -i.bak "s/<FileVersion>.*<\/FileVersion>/<FileVersion>$VERSION.0<\/FileVersion>/" src/UpdateVersionManager/UpdateVersionManager.csproj

# 執行測試
print_info "執行測試..."
if ! dotnet test test/UpdateVersionManager.Tests/UpdateVersionManager.Tests.csproj --configuration Release; then
    print_error "測試失敗"
    exit 1
fi
print_success "所有測試通過"

# 建置各平台版本
print_info "建置發布版本..."

PLATFORMS=("win-x64" "linux-x64" "osx-x64")
EXTENSIONS=(".exe" "" "")

mkdir -p "release/v$VERSION"

for i in "${!PLATFORMS[@]}"; do
    PLATFORM="${PLATFORMS[$i]}"
    EXT="${EXTENSIONS[$i]}"
    
    print_info "建置 $PLATFORM 版本..."
    
    dotnet publish src/UpdateVersionManager/UpdateVersionManager.csproj \
        -c Release \
        -r "$PLATFORM" \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:PublishTrimmed=false \
        -p:Version="$VERSION" \
        -p:AssemblyVersion="$VERSION.0" \
        -p:FileVersion="$VERSION.0" \
        -o "release/v$VERSION/$PLATFORM"
    
    # 重新命名檔案
    if [ "$PLATFORM" = "win-x64" ]; then
        mv "release/v$VERSION/$PLATFORM/uvm.exe" "release/v$VERSION/uvm-$PLATFORM-v$VERSION.exe"
        FILE_PATH="release/v$VERSION/uvm-$PLATFORM-v$VERSION.exe"
    else
        mv "release/v$VERSION/$PLATFORM/uvm" "release/v$VERSION/uvm-$PLATFORM-v$VERSION"
        chmod +x "release/v$VERSION/uvm-$PLATFORM-v$VERSION"
        FILE_PATH="release/v$VERSION/uvm-$PLATFORM-v$VERSION"
    fi
    
    # 計算 SHA256
    print_info "計算 $PLATFORM 版本 SHA256..."
    if command -v sha256sum >/dev/null 2>&1; then
        (cd "release/v$VERSION" && sha256sum "$(basename "$FILE_PATH")" > "$(basename "$FILE_PATH").sha256")
    elif command -v shasum >/dev/null 2>&1; then
        (cd "release/v$VERSION" && shasum -a 256 "$(basename "$FILE_PATH")" > "$(basename "$FILE_PATH").sha256")
    else
        print_warning "找不到 sha256sum 或 shasum 命令，跳過 SHA256 計算"
    fi
    
    # 清理建置目錄
    rm -rf "release/v$VERSION/$PLATFORM"
    
    print_success "$PLATFORM 版本建置完成"
done

# 顯示檔案大小
print_info "發布檔案:"
ls -lh "release/v$VERSION/"

# 建立並推送 tag
print_info "建立 Git tag..."
git add .
git commit -m "chore: bump version to $VERSION" || print_warning "沒有變更需要提交"
git tag -a "$TAG_NAME" -m "Release version $VERSION"

print_info "推送到遠端倉庫..."
git push origin main
git push origin "$TAG_NAME"

print_success "Tag $TAG_NAME 已建立並推送"

# 等待 GitHub Actions
print_info "GitHub Actions 將自動建立 Release"
print_info "請前往 https://github.com/$(git config --get remote.origin.url | sed 's/.*github.com[:/]\([^/]*\/[^/]*\).*/\1/' | sed 's/\.git$//')/releases 查看"

# 顯示手動建立 Release 的指令
print_info "或者您可以使用 GitHub CLI 手動建立 Release:"
echo "gh release create $TAG_NAME release/v$VERSION/* --title \"UpdateVersionManager v$VERSION\" --notes-file CHANGELOG.md"

print_success "發布流程完成！"
