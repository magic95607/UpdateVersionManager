using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using UpdateVersionManager.Models;
using UpdateVersionManager.Services;

namespace UpdateVersionManager.Tests;

public class TestBase : IDisposable
{
    protected ServiceProvider ServiceProvider { get; private set; }
    protected Mock<ILogger<T>> MockLogger<T>() => new Mock<ILogger<T>>();
    protected UpdateVersionManagerSettings TestSettings { get; private set; }
    protected string TestDataPath { get; private set; }

    public TestBase()
    {
        TestDataPath = Path.Combine(Directory.GetCurrentDirectory(), "TestData");
        Directory.CreateDirectory(TestDataPath);
        
        // 建立測試設定
        TestSettings = new UpdateVersionManagerSettings
        {
            GoogleDriveVersionListFileId = "test-file-id",
            LocalBaseDir = Path.Combine(TestDataPath, "app_versions"),
            CurrentVersionFile = Path.Combine(TestDataPath, "current_version.txt"),
            TempExtractPath = Path.Combine(TestDataPath, "temp_update"),
            ZipFilePath = Path.Combine(TestDataPath, "update.zip"),
            AppLinkName = "current",
            VerboseOutput = false
        };

        // 建立服務容器
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UpdateVersionManager:GoogleDriveVersionListFileId"] = TestSettings.GoogleDriveVersionListFileId,
                ["UpdateVersionManager:LocalBaseDir"] = TestSettings.LocalBaseDir,
                ["UpdateVersionManager:CurrentVersionFile"] = TestSettings.CurrentVersionFile,
                ["UpdateVersionManager:TempExtractPath"] = TestSettings.TempExtractPath,
                ["UpdateVersionManager:ZipFilePath"] = TestSettings.ZipFilePath,
                ["UpdateVersionManager:AppLinkName"] = TestSettings.AppLinkName,
                ["UpdateVersionManager:VerboseOutput"] = TestSettings.VerboseOutput.ToString()
            })
            .Build();

        services.Configure<UpdateVersionManagerSettings>(
            configuration.GetSection("UpdateVersionManager"));
        
        services.AddSingleton(TestSettings);
        services.AddLogging();
        
        // 註冊服務介面和實作
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IGoogleDriveService, GoogleDriveService>();
        services.AddSingleton<ISymbolicLinkService, SymbolicLinkService>();
        services.AddSingleton<VersionManager>();
        services.AddSingleton<IOutputService, OutputService>();
    }

    protected T GetRequiredService<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    protected void CleanupTestData()
    {
        try
        {
            if (Directory.Exists(TestDataPath))
            {
                Directory.Delete(TestDataPath, true);
            }
        }
        catch
        {
            // 忽略清理錯誤
        }
    }

    public virtual void Dispose()
    {
        ServiceProvider?.Dispose();
        CleanupTestData();
        GC.SuppressFinalize(this);
    }
}
