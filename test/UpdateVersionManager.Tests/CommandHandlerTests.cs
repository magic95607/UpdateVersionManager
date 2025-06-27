using UpdateVersionManager.Services;
using UpdateVersionManager.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace UpdateVersionManager.Tests;

public class CommandHandlerTests : TestBase
{
    private readonly VersionManager _versionManager;
    private readonly IOutputService _outputService;

    public CommandHandlerTests()
    {
        _versionManager = ServiceProvider.GetRequiredService<VersionManager>();
        _outputService = ServiceProvider.GetRequiredService<IOutputService>();
    }

    private string GetCapturedOutput()
    {
        var output = string.Join(Environment.NewLine, _outputService.GetCapturedOutput());
        _outputService.ClearCapturedOutput();
        return output;
    }

    [Fact]
    public async Task HandleCommand_WithHelpCommand_ShouldShowHelp()
    {
        // Act
        await CommandHandler.HandleCommand("help", Array.Empty<string>(), _versionManager, _outputService);
        
        // Assert
        var output = GetCapturedOutput();
        output.Should().Contain("命令:");
    }

    [Fact(Skip = "跳過檔案系統相關測試，避免 CI 環境中的檔案競爭問題")]
    public async Task HandleCommand_WithCurrentCommand_ShouldShowCurrentVersion()
    {
        // Arrange - 設定一個當前版本
        var currentVersionFile = TestSettings.CurrentVersionFile;
        Directory.CreateDirectory(Path.GetDirectoryName(currentVersionFile)!);
        await File.WriteAllTextAsync(currentVersionFile, "1.0.0");

        // Act
        await CommandHandler.HandleCommand("current", Array.Empty<string>(), _versionManager, _outputService);
        
        // Assert
        var output = GetCapturedOutput();
        output.Should().Contain("1.0.0");
    }

    [Fact]
    public async Task HandleCommand_WithCurrentCommand_WhenNoVersionSet_ShouldShowNoVersion()
    {
        // Arrange - 使用獨立的目錄來避免測試間干擾
        var testDir = Path.Combine(TestDataPath, "current_test_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(testDir);
        
        var testVersionFile = Path.Combine(testDir, "current_version.txt");
        
        // 確保沒有當前版本檔案
        if (File.Exists(testVersionFile))
            File.Delete(testVersionFile);
        
        // 創建一個使用獨立設置的 VersionManager
        var testSettings = new UpdateVersionManagerSettings
        {
            GoogleDriveVersionListFileId = TestSettings.GoogleDriveVersionListFileId,
            LocalBaseDir = Path.Combine(testDir, "app_versions"),
            CurrentVersionFile = testVersionFile,
            TempExtractPath = Path.Combine(testDir, "temp_update"),
            ZipFilePath = Path.Combine(testDir, "update.zip"),
            AppLinkName = "current",
            VerboseOutput = false
        };
        
        var testDownloadService = ServiceProvider.GetRequiredService<IUniversalDownloadService>();
        var testFileService = ServiceProvider.GetRequiredService<IFileService>();
        var testSymbolicLinkService = ServiceProvider.GetRequiredService<ISymbolicLinkService>();
        var testLogger = MockLogger<VersionManager>();
        var testSettingsOptions = Microsoft.Extensions.Options.Options.Create(testSettings);
        
        var testVersionManager = new VersionManager(
            testDownloadService, 
            testFileService, 
            testSymbolicLinkService, 
            testSettingsOptions, 
            testLogger.Object);
        
        // 確保狀態正確
        var currentVersion = testVersionManager.GetCurrentVersion();
        currentVersion.Should().BeNull();

        // Act
        await CommandHandler.HandleCommand("current", Array.Empty<string>(), testVersionManager, _outputService);
        
        // Assert
        var output = GetCapturedOutput();
        output.Should().Contain("尚未設定版本");
        
        // Cleanup
        try
        {
            if (Directory.Exists(testDir))
                Directory.Delete(testDir, true);
        }
        catch { /* Ignore cleanup errors */ }
    }

    [Fact]
    public async Task HandleCommand_WithListCommand_ShouldListInstalledVersions()
    {
        // Arrange
        var versions = new[] { "1.0.0", "1.1.0" };
        Directory.CreateDirectory(TestSettings.LocalBaseDir);
        
        foreach (var version in versions)
        {
            var versionDir = Path.Combine(TestSettings.LocalBaseDir, version);
            Directory.CreateDirectory(versionDir);
        }

        // Act
        await CommandHandler.HandleCommand("list", Array.Empty<string>(), _versionManager, _outputService);
        
        // Assert
        var output = GetCapturedOutput();
        foreach (var version in versions)
        {
            output.Should().Contain(version);
        }
    }

    [Fact]
    public async Task HandleCommand_WithListCommand_WhenNoVersionsInstalled_ShouldShowNoVersions()
    {
        // Arrange - 確保目錄為空
        if (Directory.Exists(TestSettings.LocalBaseDir))
            Directory.Delete(TestSettings.LocalBaseDir, true);

        // Act
        await CommandHandler.HandleCommand("list", Array.Empty<string>(), _versionManager, _outputService);
        
        // Assert
        var output = GetCapturedOutput();
        output.Should().Contain("(無已安裝版本)");
    }

    [Fact]
    public async Task HandleCommand_WithInstallCommand_WithoutVersion_ShouldShowUsage()
    {
        // Act
        await CommandHandler.HandleCommand("install", Array.Empty<string>(), _versionManager, _outputService);
        
        // Assert
        var output = GetCapturedOutput();
        output.Should().Contain("使用方式: uvm install <version>");
    }

    [Fact]
    public async Task HandleCommand_WithUseCommand_WithoutVersion_ShouldShowUsage()
    {
        // Act
        await CommandHandler.HandleCommand("use", Array.Empty<string>(), _versionManager, _outputService);
        
        // Assert
        var output = GetCapturedOutput();
        output.Should().Contain("使用方式: uvm use <version>");
    }

    [Fact]
    public async Task HandleCommand_WithCleanCommand_WithoutVersion_ShouldShowUsage()
    {
        // Act
        await CommandHandler.HandleCommand("clean", Array.Empty<string>(), _versionManager, _outputService);
        
        // Assert
        var output = GetCapturedOutput();
        output.Should().Contain("使用方式: uvm clean <version>");
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("HELP")]
    public async Task HandleCommand_WithHelpAliases_ShouldWork(string command)
    {
        // Act
        await CommandHandler.HandleCommand(command, Array.Empty<string>(), _versionManager, _outputService);
        
        // Assert
        var output = GetCapturedOutput();
        output.Should().Contain("uvm - 版本管理工具");
    }

    [Theory(Skip = "Directory-based tests are unstable in parallel execution and CI environments")]
    [InlineData("List")]
    [InlineData("LIST")]
    [InlineData("ls")]
    public async Task HandleCommand_WithListAliases_ShouldWork(string command)
    {
        // Arrange
        Directory.CreateDirectory(TestSettings.LocalBaseDir);
        Directory.CreateDirectory(Path.Combine(TestSettings.LocalBaseDir, "1.0.0"));

        // Act
        await CommandHandler.HandleCommand(command, Array.Empty<string>(), _versionManager, _outputService);
        
        // Assert
        var output = GetCapturedOutput();
        output.Should().Contain("1.0.0");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("unknown")]
    [InlineData("test")]
    public async Task HandleCommand_WithUnknownCommand_ShouldShowUnknownAndHelp(string unknownCommand)
    {
        // Act
        await CommandHandler.HandleCommand(unknownCommand, Array.Empty<string>(), _versionManager, _outputService);
        
        // Assert
        var output = GetCapturedOutput();
        output.Should().Contain($"未知命令: {unknownCommand}");
        output.Should().Contain("uvm - 版本管理工具");
    }
}
