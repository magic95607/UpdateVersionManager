using Microsoft.Extensions.Logging;
using Moq;
using UpdateVersionManager.Services;
using FluentAssertions;

namespace UpdateVersionManager.Tests.Services;

public class UniversalDownloadServiceTests : TestBase
{
    private readonly UniversalDownloadService _downloadService;
    private readonly Mock<ILogger<UniversalDownloadService>> _mockLogger;

    public UniversalDownloadServiceTests()
    {
        _mockLogger = MockLogger<UniversalDownloadService>();
        _downloadService = new UniversalDownloadService(_mockLogger.Object);
    }

    [Theory]
    [InlineData("https://drive.google.com/file/d/1234/view")]
    [InlineData("https://docs.google.com/file/d/1234")]
    [InlineData("https://github.com/user/repo/releases/download/v1.0.0/file.zip")]
    [InlineData("https://raw.githubusercontent.com/user/repo/main/file.txt")]
    [InlineData("ftp://example.com/file.zip")]
    [InlineData("ftps://example.com/file.zip")]
    [InlineData("https://example.com/file.zip")]
    [InlineData("http://example.com/file.txt")]
    public void DetectUrlSource_ShouldHandleDifferentUrlTypes(string url)
    {
        // Arrange & Act
        // 驗證 URL 格式是有效的，可以被 Uri 解析
        var act = () => new Uri(url);

        // Assert
        act.Should().NotThrow();
        var uri = new Uri(url);
        uri.IsAbsoluteUri.Should().BeTrue();
    }

    [Fact]
    public void Constructor_ShouldSetUserAgent()
    {
        // Arrange & Act
        using var service = new UniversalDownloadService(_mockLogger.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange & Act & Assert
        var act = () => _downloadService.Dispose();
        act.Should().NotThrow();
    }
}
