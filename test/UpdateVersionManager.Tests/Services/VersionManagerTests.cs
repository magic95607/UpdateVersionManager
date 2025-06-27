using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using UpdateVersionManager.Models;
using UpdateVersionManager.Services;
using FluentAssertions;

namespace UpdateVersionManager.Tests.Services;

public class VersionManagerTests : TestBase
{
    private readonly VersionManager _versionManager;
    private readonly Mock<IGoogleDriveService> _mockGoogleDriveService;
    private readonly Mock<IFileService> _mockFileService;
    private readonly Mock<ISymbolicLinkService> _mockSymbolicLinkService;
    private readonly Mock<ILogger<VersionManager>> _mockLogger;

    public VersionManagerTests()
    {
        _mockGoogleDriveService = new Mock<IGoogleDriveService>();
        _mockFileService = new Mock<IFileService>();
        _mockSymbolicLinkService = new Mock<ISymbolicLinkService>();
        _mockLogger = MockLogger<VersionManager>();

        var options = Options.Create(TestSettings);
        
        _versionManager = new VersionManager(
            _mockGoogleDriveService.Object,
            _mockFileService.Object,
            _mockSymbolicLinkService.Object,
            options,
            _mockLogger.Object);
    }

    [Fact]
    public void GetCurrentVersion_WhenFileExists_ShouldReturnVersion()
    {
        // Arrange
        const string expectedVersion = "1.2.0";
        Directory.CreateDirectory(Path.GetDirectoryName(TestSettings.CurrentVersionFile)!);
        File.WriteAllText(TestSettings.CurrentVersionFile, $"  {expectedVersion}  \n");

        // Act
        var currentVersion = _versionManager.GetCurrentVersion();

        // Assert
        currentVersion.Should().Be(expectedVersion);
    }

    [Fact]
    public void GetCurrentVersion_WhenFileDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        // 確保檔案不存在
        if (File.Exists(TestSettings.CurrentVersionFile))
            File.Delete(TestSettings.CurrentVersionFile);

        // Act
        var currentVersion = _versionManager.GetCurrentVersion();

        // Assert
        currentVersion.Should().BeNull();
    }

    [Fact]
    public void GetInstalledVersions_WhenDirectoryExists_ShouldReturnVersionList()
    {
        // Arrange
        var expectedVersions = new[] { "1.0.0", "1.1.0", "1.2.0" };
        
        // 清理並重新建立測試目錄
        if (Directory.Exists(TestSettings.LocalBaseDir))
            Directory.Delete(TestSettings.LocalBaseDir, true);
            
        Directory.CreateDirectory(TestSettings.LocalBaseDir);
        
        foreach (var version in expectedVersions)
        {
            var versionDir = Path.Combine(TestSettings.LocalBaseDir, version);
            Directory.CreateDirectory(versionDir);
            File.WriteAllText(Path.Combine(versionDir, "dummy.txt"), "test");
        }

        // Act
        var installedVersions = _versionManager.GetInstalledVersions();

        // Assert
        installedVersions.Should().HaveCount(3);
        installedVersions.Should().BeEquivalentTo(expectedVersions.Reverse()); // 應該按降序排列
    }

    [Fact]
    public void GetInstalledVersions_WhenDirectoryDoesNotExist_ShouldReturnEmptyList()
    {
        // Arrange
        // 確保目錄不存在
        if (Directory.Exists(TestSettings.LocalBaseDir))
            Directory.Delete(TestSettings.LocalBaseDir, true);

        // Act
        var installedVersions = _versionManager.GetInstalledVersions();

        // Assert
        installedVersions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRemoteVersionsAsync_ShouldCallGoogleDriveService()
    {
        // Arrange
        var expectedVersions = new List<VersionInfo>
        {
            new() { Version = "1.0.0", ReleaseDate = "2025-06-26", Description = "Version 1.0.0", DownloadUrl = "test-url-1" },
            new() { Version = "1.1.0", ReleaseDate = "2025-06-27", Description = "Version 1.1.0", DownloadUrl = "test-url-2" }
        };

        // 模擬 GoogleDriveService.DownloadTextAsync 回傳 JSON 字串
        var jsonResponse = @"{
            ""versions"": [
                { ""version"": ""1.0.0"", ""downloadUrl"": ""test-url-1"", ""releaseDate"": ""2025-06-26"", ""description"": ""Version 1.0.0"" },
                { ""version"": ""1.1.0"", ""downloadUrl"": ""test-url-2"", ""releaseDate"": ""2025-06-27"", ""description"": ""Version 1.1.0"" }
            ]
        }";

        _mockGoogleDriveService
            .Setup(x => x.DownloadTextAsync(It.IsAny<string>()))
            .ReturnsAsync(jsonResponse);

        // Act
        var remoteVersions = await _versionManager.GetRemoteVersionsAsync();

        // Assert
        remoteVersions.Should().HaveCount(2);
        remoteVersions[0].Version.Should().Be("1.0.0");
        remoteVersions[1].Version.Should().Be("1.1.0");
        _mockGoogleDriveService.Verify(x => x.DownloadTextAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task InstallVersionAsync_WithValidVersion_ShouldInstallSuccessfully()
    {
        // Arrange
        const string version = "1.0.0";
        var jsonResponse = @"{
            ""versions"": [
                { ""version"": ""1.0.0"", ""downloadUrl"": ""test-download-url"", ""sha256"": ""test-hash"", ""size"": 1024 }
            ]
        }";

        _mockGoogleDriveService
            .Setup(x => x.DownloadTextAsync(It.IsAny<string>()))
            .ReturnsAsync(jsonResponse);

        _mockGoogleDriveService
            .Setup(x => x.DownloadFileAsync("test-download-url", TestSettings.ZipFilePath))
            .Returns(Task.CompletedTask);

        _mockFileService
            .Setup(x => x.CalculateFileHashAsync(TestSettings.ZipFilePath))
            .ReturnsAsync("test-hash");

        // Act
        await _versionManager.InstallVersionAsync(version);

        // Assert
        _mockGoogleDriveService.Verify(x => x.DownloadTextAsync(It.IsAny<string>()), Times.Once);
        _mockGoogleDriveService.Verify(x => x.DownloadFileAsync("test-download-url", TestSettings.ZipFilePath), Times.Once);
        _mockFileService.Verify(x => x.VerifyFileHashAsync(TestSettings.ZipFilePath, "test-hash"), Times.Once);
    }

    [Fact]
    public async Task InstallVersionAsync_WithNonExistentVersion_ShouldNotThrowException()
    {
        // Arrange
        const string version = "99.99.99";
        var jsonResponse = @"{
            ""versions"": [
                { ""version"": ""1.0.0"", ""downloadUrl"": ""test-url"" }
            ]
        }";

        _mockGoogleDriveService
            .Setup(x => x.DownloadTextAsync(It.IsAny<string>()))
            .ReturnsAsync(jsonResponse);

        // Act
        await _versionManager.InstallVersionAsync(version);

        // Assert - 不應該拋出異常，而是優雅地處理
        _mockGoogleDriveService.Verify(x => x.DownloadTextAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task UseVersionAsync_WithInstalledVersion_ShouldCallUpdateAppLinkAsync()
    {
        // Arrange
        const string version = "1.0.0";
        var versionPath = Path.Combine(TestSettings.LocalBaseDir, version);
        
        // 建立版本目錄模擬已安裝版本
        Directory.CreateDirectory(versionPath);

        _mockSymbolicLinkService
            .Setup(x => x.UpdateAppLinkAsync(version, versionPath, It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _versionManager.UseVersionAsync(version);

        // Assert
        _mockSymbolicLinkService.Verify(
            x => x.UpdateAppLinkAsync(version, versionPath, It.IsAny<string>()), 
            Times.Once);
        
        // 驗證版本檔案是否寫入
        File.Exists(TestSettings.CurrentVersionFile).Should().BeTrue();
        var savedVersion = File.ReadAllText(TestSettings.CurrentVersionFile).Trim();
        savedVersion.Should().Be(version);
    }

    [Fact]
    public async Task UseVersionAsync_WithNonInstalledVersion_ShouldNotThrowException()
    {
        // Arrange
        const string version = "99.99.99";
        
        // 確保版本目錄不存在
        var versionPath = Path.Combine(TestSettings.LocalBaseDir, version);
        if (Directory.Exists(versionPath))
            Directory.Delete(versionPath, true);

        // Act
        await _versionManager.UseVersionAsync(version);

        // Assert - 不應該拋出異常，而是優雅地處理
        // 可以檢查 console 輸出或日誌，但這裡我們只確保沒有異常
        Assert.True(true); // 如果沒有異常拋出，測試就算通過
    }

    [Fact]
    public async Task CleanVersionAsync_WithInstalledVersion_ShouldRemoveVersion()
    {
        // Arrange
        const string version = "1.0.0";
        var versionPath = Path.Combine(TestSettings.LocalBaseDir, version);
        
        // 確保父目錄存在
        Directory.CreateDirectory(TestSettings.LocalBaseDir);
        
        // 建立版本目錄
        Directory.CreateDirectory(versionPath);
        File.WriteAllText(Path.Combine(versionPath, "test.txt"), "test content");
        
        // 確認目錄已建立
        Directory.Exists(versionPath).Should().BeTrue();

        // Act
        await _versionManager.CleanVersionAsync(version);

        // Assert
        Directory.Exists(versionPath).Should().BeFalse();
    }

    [Fact]
    public async Task CleanVersionAsync_WithNonInstalledVersion_ShouldNotThrowException()
    {
        // Arrange
        const string version = "99.99.99";
        
        // 確保版本目錄不存在
        var versionPath = Path.Combine(TestSettings.LocalBaseDir, version);
        if (Directory.Exists(versionPath))
            Directory.Delete(versionPath, true);

        // Act
        await _versionManager.CleanVersionAsync(version);

        // Assert - 不應該拋出異常，而是優雅地處理
        Assert.True(true); // 如果沒有異常拋出，測試就算通過
    }
}
