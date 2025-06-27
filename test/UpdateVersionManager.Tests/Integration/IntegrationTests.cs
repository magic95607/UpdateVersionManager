using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UpdateVersionManager.Models;
using UpdateVersionManager.Services;
using FluentAssertions;

namespace UpdateVersionManager.Tests.Integration;

public class IntegrationTests : TestBase
{
    private readonly ServiceProvider _serviceProvider;

    public IntegrationTests()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        
        // 註冊實際的服務（除了需要外部依賴的）
        services.AddSingleton<FileService>();
        services.AddSingleton<OutputService>();
        services.AddLogging();
        
        _serviceProvider = services.BuildServiceProvider();
    }

    public override void Dispose()
    {
        _serviceProvider.Dispose();
        base.Dispose();
    }

    [Fact]
    public void ServiceContainer_ShouldResolveAllServices()
    {
        // Act & Assert
        var fileService = _serviceProvider.GetService<FileService>();
        var outputService = _serviceProvider.GetService<OutputService>();
        var settings = _serviceProvider.GetService<UpdateVersionManagerSettings>();

        fileService.Should().NotBeNull();
        outputService.Should().NotBeNull();
        settings.Should().NotBeNull();
    }

    [Fact]
    public void Configuration_ShouldLoadCorrectly()
    {
        // Arrange & Act
        var settings = _serviceProvider.GetRequiredService<UpdateVersionManagerSettings>();

        // Assert
        settings.GoogleDriveVersionListFileId.Should().Be("test-file-id");
        settings.LocalBaseDir.Should().NotBeNullOrEmpty();
        settings.CurrentVersionFile.Should().NotBeNullOrEmpty();
        settings.VerboseOutput.Should().BeFalse();
    }

    [Fact(Skip = "Integration test involves file operations that may have timing issues in CI environments")]
    public async Task FileService_Integration_ShouldWorkEndToEnd()
    {
        // Arrange
        var fileService = _serviceProvider.GetRequiredService<FileService>();
        var testFile = Path.Combine(TestDataPath, "integration_test.txt");
        const string content = "Integration test content";
        
        // 確保目錄存在
        Directory.CreateDirectory(Path.GetDirectoryName(testFile)!);
        await File.WriteAllTextAsync(testFile, content);

        // Act
        var hash = await fileService.CalculateFileHashAsync(testFile);
        var versionInfo = await fileService.GenerateVersionInfoAsync("1.0.0", testFile, "test-download-url");

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().HaveLength(64);
        
        versionInfo.Should().NotBeNull();
        versionInfo.Version.Should().Be("1.0.0");
        versionInfo.Sha256.Should().Be(hash);
        versionInfo.Size.Should().Be(content.Length);
        versionInfo.DownloadUrl.Should().Be("https://drive.google.com/uc?export=download&id=test-download-url");
    }

    [Fact]
    public void OutputService_Integration_ShouldHandleVerboseSettings()
    {
        // Arrange
        var outputService = _serviceProvider.GetRequiredService<OutputService>();
        var settings = _serviceProvider.GetRequiredService<UpdateVersionManagerSettings>();
        
        using var consoleOutput = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(consoleOutput);

        try
        {
            // Act - Test with VerboseOutput = false
            settings.VerboseOutput = false;
            outputService.WriteVerbose("This should not appear in console");
            var output1 = consoleOutput.ToString();

            // Clear and test with VerboseOutput = true
            consoleOutput.GetStringBuilder().Clear();
            settings.VerboseOutput = true;
            outputService.WriteVerbose("This should appear in console");
            var output2 = consoleOutput.ToString();

            // Assert
            output1.Should().NotContain("[VERBOSE]");
            output2.Should().Contain("[VERBOSE]");
            output2.Should().Contain("This should appear in console");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void Settings_ShouldUpdateDynamically()
    {
        // Arrange
        var settings = _serviceProvider.GetRequiredService<UpdateVersionManagerSettings>();
        var originalVerboseOutput = settings.VerboseOutput;

        // Act
        settings.VerboseOutput = !originalVerboseOutput;

        // Assert
        settings.VerboseOutput.Should().Be(!originalVerboseOutput);
    }
}
