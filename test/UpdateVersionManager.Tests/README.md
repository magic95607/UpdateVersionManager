# UpdateVersionManager Tests

This project contains comprehensive unit and integration tests for the UpdateVersionManager application.

## Overview

UpdateVersionManager is a .NET 9 console application for managing software versions with Google Drive integration. This test project ensures all functionality works correctly and maintains high code quality.

## Test Structure

```
UpdateVersionManager.Tests/
├── Services/
│   ├── OutputServiceTests.cs          # Tests for output and logging service
│   ├── FileServiceTests.cs            # Tests for file operations
│   ├── VersionManagerTests.cs         # Tests for core version management
│   └── ...
├── Integration/
│   └── IntegrationTests.cs            # End-to-end integration tests
├── TestData/
│   └── sample_versions.json           # Test data files
├── CommandHandlerTests.cs             # Tests for CLI command handling
└── TestBase.cs                        # Base class for all tests
```

## Features Tested

### 🔧 Core Services
- **OutputService**: Console/Log output separation, Verbose mode control
- **FileService**: SHA256 calculation, ZIP extraction, JSON handling
- **VersionManager**: Version installation, switching, cleanup
- **SymbolicLinkService**: Symbolic link creation and management
- **UniversalDownloadService**: Remote version list retrieval, file downloads from multiple sources (Google Drive, GitHub, FTP)

### 📋 Command Interface
- **CLI Commands**: help, list, install, use, current, update, clean, hash, generate
- **Parameter Validation**: Required parameters and error handling
- **Command Aliases**: Short forms and case insensitivity

### ⚙️ Configuration
- **Settings Loading**: appsettings.json and environment-specific configs
- **VerboseOutput Control**: Production vs Development output behavior
- **Dependency Injection**: Service resolution and configuration binding

## Running Tests

### All Tests
```bash
dotnet test
```

### Specific Test Categories
```bash
# Unit tests only
dotnet test --filter Category=Unit

# Integration tests only  
dotnet test --filter Category=Integration

# Tests for specific service
dotnet test --filter FullyQualifiedName~OutputServiceTests
```

### With Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Test Technologies

- **xUnit**: Primary testing framework
- **Moq**: Mocking framework for dependencies
- **FluentAssertions**: Readable assertion library
- **Microsoft.Extensions.Testing**: .NET testing extensions
- **Microsoft.Extensions.Logging.Testing**: Logger testing utilities

## Test Patterns

### Test Naming Convention
```
MethodName_Condition_ExpectedResult
```

Examples:
- `WriteVerbose_WhenVerboseOutputTrue_ShouldWriteToConsoleAndLog`
- `InstallVersionAsync_WithNonExistentVersion_ShouldThrowException`

### Test Structure (AAA Pattern)
```csharp
[Fact]
public async Task MethodName_Condition_ExpectedResult()
{
    // Arrange
    // Set up test data and mocks
    
    // Act  
    // Execute the method being tested
    
    // Assert
    // Verify the expected outcomes
}
```

### Common Test Scenarios

#### Success Cases
- Valid input parameters
- Expected workflow completion
- Correct output generation

#### Error Cases  
- Invalid parameters
- Missing files/directories
- Network/external service failures
- Permission issues

#### Edge Cases
- Empty/null inputs
- Boundary values
- Concurrent operations

## Mocking Strategy

### External Dependencies
```csharp
// Mock external services
var mockDownloadService = new Mock<UniversalDownloadService>();
var mockFileService = new Mock<FileService>();

// Setup expected behavior
mockGoogleDriveService
    .Setup(x => x.GetVersionListAsync())
    .ReturnsAsync(expectedVersions);
```

### File System Operations
```csharp
// Use temporary directories for file operations
var testDataPath = Path.Combine(Directory.GetCurrentDirectory(), "TestData");
Directory.CreateDirectory(testDataPath);

// Clean up in disposal
Directory.Delete(testDataPath, true);
```

### Console Output Capture
```csharp
// Capture console output for verification
using var consoleOutput = new StringWriter();
var originalOut = Console.Out;
Console.SetOut(consoleOutput);

// ... perform actions that write to console

// Verify output
consoleOutput.ToString().Should().Contain("expected message");
```

## Continuous Integration

Tests are designed to run in CI/CD environments with:
- No external dependencies
- Temporary file cleanup
- Cross-platform compatibility
- Parallel execution support

## Contributing

When adding new features:
1. Write tests first (TDD approach)
2. Ensure both positive and negative test cases
3. Mock external dependencies appropriately  
4. Update this README if adding new test categories
5. Maintain high test coverage (>90%)

## Test Data

The `TestData` directory contains:
- Sample configuration files
- Mock version data (JSON)
- Test files for hash calculations
- ZIP files for extraction testing

All test data is cleaned up automatically after test execution.
