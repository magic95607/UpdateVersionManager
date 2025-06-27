<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

# UpdateVersionManager Test Project Instructions

This is a comprehensive unit test project for the UpdateVersionManager .NET 9 Console application.

## Project Structure

- **Services Tests**: Unit tests for all service classes (VersionManager, FileService, GoogleDriveService, SymbolicLinkService, OutputService)
- **Models Tests**: Tests for data models and configuration classes
- **Integration Tests**: End-to-end tests that verify service interactions
- **CommandHandler Tests**: Tests for command-line interface logic

## Testing Guidelines

1. **Use xUnit** as the primary testing framework
2. **Use Moq** for mocking dependencies
3. **Use FluentAssertions** for more readable assertions
4. **Inherit from TestBase** for common test setup and cleanup
5. **Use descriptive test names** following the pattern: `MethodName_Condition_ExpectedResult`
6. **Test both success and failure scenarios**
7. **Mock external dependencies** (file system, network calls) appropriately
8. **Clean up test data** in disposal methods

## Key Testing Areas

- **Output Service Logic**: Verify VerboseOutput behavior and Console/Log separation
- **File Operations**: Hash calculations, zip extraction, file I/O
- **Version Management**: Installation, switching, cleanup of versions
- **Command Handling**: All CLI commands and their parameters
- **Configuration**: Settings loading and environment-specific behavior
- **Error Handling**: Exception scenarios and error messages

## Test Data Management

- Use the `TestData` directory for sample files
- Clean up temporary files in test disposal
- Use in-memory configuration for testing
- Mock external API calls (Google Drive)

## Mocking Strategy

- Mock external services (GoogleDriveService) for unit tests
- Use real implementations for integration tests where appropriate
- Mock ILogger for capturing log output verification
- Use temporary directories for file system operations

When writing tests, ensure comprehensive coverage of both happy path and edge cases, and maintain clean, readable test code that clearly expresses intent.
