using UpdateVersionManager.Services;
using UpdateVersionManager.Models;
using FluentAssertions;
using System.Text.Json;

namespace UpdateVersionManager.Tests.Services;

public class FileServiceTests : TestBase
{
    private readonly FileService _fileService;
    private readonly string _testFile;

    public FileServiceTests()
    {
        _fileService = new FileService();
        _testFile = Path.Combine(TestDataPath, "test.txt");
    }

    [Fact]
    public async Task CalculateFileHashAsync_WithValidFile_ShouldReturnCorrectHash()
    {
        // Arrange
        const string content = "Hello, World!";
        await File.WriteAllTextAsync(_testFile, content);

        // Act
        var hash = await _fileService.CalculateFileHashAsync(_testFile);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().HaveLength(64); // SHA256 hash length
        hash.Should().Be("dffd6021bb2bd5b0af676290809ec3a53191dd81c7f70a4b28688a362182986f");
    }

    [Fact]
    public async Task CalculateFileHashAsync_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        const string nonExistentFile = "non_existent_file.txt";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => 
            _fileService.CalculateFileHashAsync(nonExistentFile));
    }

    [Fact]
    public async Task GenerateVersionInfoAsync_WithValidParameters_ShouldReturnVersionInfo()
    {
        // Arrange
        const string version = "1.0.0";
        const string content = "Test file content";
        const string downloadUrl = "test-download-url";
        
        await File.WriteAllTextAsync(_testFile, content);

        // Act
        var versionInfo = await _fileService.GenerateVersionInfoAsync(version, _testFile, downloadUrl);

        // Assert
        versionInfo.Should().NotBeNull();
        versionInfo.Version.Should().Be(version);
        versionInfo.DownloadUrl.Should().Be($"https://drive.google.com/uc?export=download&id={downloadUrl}");
        versionInfo.Sha256.Should().NotBeNullOrEmpty();
        versionInfo.Size.Should().Be(content.Length);
        versionInfo.ReleaseDate.Should().Be(DateTime.Now.ToString("yyyy-MM-dd"));
        versionInfo.Description.Should().Be($"Version {version}");
    }

    [Fact]
    public async Task SaveVersionInfoToJsonAsync_ShouldCreateJsonFile()
    {
        // Arrange
        var versionInfo = new VersionInfo
        {
            Version = "1.0.0",
            DownloadUrl = "test-download-url",
            Sha256 = "test-hash",
            Size = 1024,
            ReleaseDate = "2025-06-27",
            Description = "Version 1.0.0"
        };

        // Act
        await _fileService.SaveVersionInfoToJsonAsync(versionInfo);

        // Assert
        var jsonFile = Path.Combine(Directory.GetCurrentDirectory(), "version_item.json");
        File.Exists(jsonFile).Should().BeTrue();

        var jsonContent = await File.ReadAllTextAsync(jsonFile);
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var deserializedVersionInfo = JsonSerializer.Deserialize<VersionInfo>(jsonContent, jsonOptions);

        deserializedVersionInfo.Should().NotBeNull();
        deserializedVersionInfo!.Version.Should().Be(versionInfo.Version);
        deserializedVersionInfo.DownloadUrl.Should().Be(versionInfo.DownloadUrl);
        deserializedVersionInfo.Sha256.Should().Be(versionInfo.Sha256);
        deserializedVersionInfo.Size.Should().Be(versionInfo.Size);
        deserializedVersionInfo.ReleaseDate.Should().Be(versionInfo.ReleaseDate);
        deserializedVersionInfo.Description.Should().Be(versionInfo.Description);

        // Cleanup
        if (File.Exists(jsonFile))
            File.Delete(jsonFile);
    }

    [Fact]
    public async Task VerifyFileHashAsync_WithCorrectHash_ShouldReturnTrue()
    {
        // Arrange
        const string content = "Test content for verification";
        await File.WriteAllTextAsync(_testFile, content);
        
        var hash = await _fileService.CalculateFileHashAsync(_testFile);

        // Act
        var result = await _fileService.VerifyFileHashAsync(_testFile, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyFileHashAsync_WithIncorrectHash_ShouldReturnFalse()
    {
        // Arrange
        const string content = "Test content for verification";
        await File.WriteAllTextAsync(_testFile, content);
        
        const string incorrectHash = "incorrect-hash";

        // Act
        var result = await _fileService.VerifyFileHashAsync(_testFile, incorrectHash);

        // Assert
        result.Should().BeFalse();
    }
}
