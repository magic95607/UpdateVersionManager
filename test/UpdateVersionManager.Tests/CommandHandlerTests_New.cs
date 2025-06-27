using UpdateVersionManager.Services;
using FluentAssertions;
using System.IO;
using Microsoft.Extensions.DependencyInjection;

namespace UpdateVersionManager.Tests;

public class CommandHandlerTestsNew : TestBase
{
    private readonly VersionManager _versionManager;
    private readonly IOutputService _outputService;

    public CommandHandlerTestsNew()
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
        output.Should().Contain("uvm - 版本管理工具");
        output.Should().Contain("命令:");
    }

    [Fact]
    public async Task HandleCommand_WithCurrentCommand_ShouldShowCurrentVersion()
    {
        // Arrange - 設定一個當前版本
        Directory.CreateDirectory(Path.GetDirectoryName(TestSettings.CurrentVersionFile)!);
        await File.WriteAllTextAsync(TestSettings.CurrentVersionFile, "1.0.0");

        // Act
        await CommandHandler.HandleCommand("current", Array.Empty<string>(), _versionManager, _outputService);

        // Assert
        var output = GetCapturedOutput();
        output.Should().Contain("1.0.0");
    }

    [Fact]
    public async Task HandleCommand_WithCurrentCommand_WhenNoVersionSet_ShouldShowNoVersion()
    {
        // Arrange - 確保沒有當前版本檔案
        if (File.Exists(TestSettings.CurrentVersionFile))
            File.Delete(TestSettings.CurrentVersionFile);

        // Act
        await CommandHandler.HandleCommand("current", Array.Empty<string>(), _versionManager, _outputService);

        // Assert
        var output = GetCapturedOutput();
        output.Should().Contain("尚未設定版本");
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
        output.Should().Contain("尚未安裝任何版本");
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

    [Theory]
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
    }
}
