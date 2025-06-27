using Microsoft.Extensions.Logging;
using Moq;
using UpdateVersionManager.Models;
using UpdateVersionManager.Services;
using FluentAssertions;

namespace UpdateVersionManager.Tests.Services;

public class OutputServiceTests : TestBase
{
    private readonly OutputService _outputService;
    private readonly Mock<ILogger<OutputService>> _mockLogger;
    private readonly StringWriter _consoleOutput;
    private readonly TextWriter _originalConsoleOut;

    public OutputServiceTests()
    {
        _mockLogger = MockLogger<OutputService>();
        _consoleOutput = new StringWriter();
        _originalConsoleOut = Console.Out;
        Console.SetOut(_consoleOutput);
        
        _outputService = new OutputService(_mockLogger.Object, TestSettings);
    }

    public override void Dispose()
    {
        Console.SetOut(_originalConsoleOut);
        _consoleOutput.Dispose();
        base.Dispose();
    }

    [Fact]
    public void WriteInfo_ShouldWriteToConsoleAndLog()
    {
        // Arrange
        const string message = "Test info message";

        // Act
        _outputService.WriteInfo(message);

        // Assert
        _consoleOutput.ToString().Should().Contain(message);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void WriteError_ShouldWriteToConsoleAndLog()
    {
        // Arrange
        const string message = "Test error message";

        // Act
        _outputService.WriteError(message);

        // Assert
        _consoleOutput.ToString().Should().Contain(message);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void WriteError_WithException_ShouldWriteToConsoleAndLog()
    {
        // Arrange
        const string message = "Test error message";
        var exception = new InvalidOperationException("Test exception");

        // Act
        _outputService.WriteError(message, exception);

        // Assert
        _consoleOutput.ToString().Should().Contain(message);
        _consoleOutput.ToString().Should().Contain(exception.Message);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void WriteVerbose_WhenVerboseOutputFalse_ShouldNotWriteToConsoleButShouldLog()
    {
        // Arrange
        const string message = "Test verbose message";
        TestSettings.VerboseOutput = false;

        // Act
        _outputService.WriteVerbose(message);

        // Assert
        _consoleOutput.ToString().Should().NotContain("[VERBOSE]");
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void WriteVerbose_WhenVerboseOutputTrue_ShouldWriteToConsoleAndLog()
    {
        // Arrange
        const string message = "Test verbose message";
        TestSettings.VerboseOutput = true;

        // Act
        _outputService.WriteVerbose(message);

        // Assert
        _consoleOutput.ToString().Should().Contain("[VERBOSE]");
        _consoleOutput.ToString().Should().Contain(message);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void WriteDebug_WhenVerboseOutputFalse_ShouldNotWriteToConsoleButShouldLog()
    {
        // Arrange
        const string message = "Test debug message";
        TestSettings.VerboseOutput = false;

        // Act
        _outputService.WriteDebug(message);

        // Assert
        _consoleOutput.ToString().Should().NotContain("[DEBUG]");
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void WriteDebug_WhenVerboseOutputTrue_ShouldWriteToConsoleAndLog()
    {
        // Arrange
        const string message = "Test debug message";
        TestSettings.VerboseOutput = true;

        // Act
        _outputService.WriteDebug(message);

        // Assert
        _consoleOutput.ToString().Should().Contain("[DEBUG]");
        _consoleOutput.ToString().Should().Contain(message);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void WriteConsoleOnly_ShouldOnlyWriteToConsole()
    {
        // Arrange
        const string message = "Test console only message";

        // Act
        _outputService.WriteConsoleOnly(message);

        // Assert
        _consoleOutput.ToString().Should().Contain(message);
        _mockLogger.VerifyNoOtherCalls();
    }

    [Fact]
    public void LogOnly_ShouldOnlyWriteToLog()
    {
        // Arrange
        const string message = "Test log only message";

        // Act
        _outputService.LogOnly(LogLevel.Information, message);

        // Assert
        _consoleOutput.ToString().Should().NotContain(message);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
