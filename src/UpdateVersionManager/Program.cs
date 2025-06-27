using UpdateVersionManager;
using UpdateVersionManager.Services;
using UpdateVersionManager.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using System.Text;
using System.Reflection;

// 設定 Console 編碼為 UTF-8
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

// 建立設定
var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
var configuration = BuildConfiguration(environment);

// 輔助方法：建立設定
static IConfiguration BuildConfiguration(string environment)
{
    var builder = new ConfigurationBuilder();
    
    // 嘗試從檔案讀取（開發時）
    var basePath = AppContext.BaseDirectory;
    var mainConfigPath = Path.Combine(basePath, "appsettings.json");
    var envConfigPath = Path.Combine(basePath, $"appsettings.{environment}.json");
    
    if (File.Exists(mainConfigPath))
    {
        builder.AddJsonFile(mainConfigPath, optional: false, reloadOnChange: true);
    }
    else
    {
        // 從嵌入資源讀取（發佈後）
        var mainStream = GetEmbeddedResourceStream("appsettings.json");
        if (mainStream != null)
        {
            builder.AddJsonStream(mainStream);
        }
    }
    
    if (File.Exists(envConfigPath))
    {
        builder.AddJsonFile(envConfigPath, optional: true, reloadOnChange: true);
    }
    else
    {
        // 從嵌入資源讀取環境特定設定
        var envStream = GetEmbeddedResourceStream($"appsettings.{environment}.json");
        if (envStream != null)
        {
            builder.AddJsonStream(envStream);
        }
    }
    
    builder.AddEnvironmentVariables();
    return builder.Build();
}

// 輔助方法：取得嵌入資源流
static Stream? GetEmbeddedResourceStream(string resourceName)
{
    var assembly = Assembly.GetExecutingAssembly();
    var fullResourceName = $"UpdateVersionManager.{resourceName}";
    return assembly.GetManifestResourceStream(fullResourceName);
}

// 設定 Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

try
{
    Log.Information("UpdateVersionManager 啟動中...");

    // 設定服務
    var services = new ServiceCollection();
    services.Configure<UpdateVersionManagerSettings>(
        configuration.GetSection("UpdateVersionManager"));
    services.AddSingleton<IFileService, FileService>();
    services.AddSingleton<IGoogleDriveService, GoogleDriveService>();
    services.AddSingleton<ISymbolicLinkService, SymbolicLinkService>();
    services.AddSingleton<VersionManager>();
    services.AddSingleton<IOutputService, OutputService>();
    services.AddSingleton(provider => 
        provider.GetRequiredService<IOptions<UpdateVersionManagerSettings>>().Value);
    services.AddLogging(builder => builder.AddSerilog());

    var serviceProvider = services.BuildServiceProvider();

    // 解析命令行參數
    var commandLineArgs = Environment.GetCommandLineArgs();
    var versionManager = serviceProvider.GetRequiredService<VersionManager>();
    var output = serviceProvider.GetRequiredService<IOutputService>();

    if (commandLineArgs.Length > 1)
    {
        Log.Debug("執行命令: {Command} {Parameters}", commandLineArgs[1], string.Join(" ", commandLineArgs.Skip(2)));
        await CommandHandler.HandleCommand(commandLineArgs[1], commandLineArgs.Skip(2).ToArray(), versionManager, output);
        return;
    }

    Log.Information("執行自動更新");
    // 預設行為：自動更新
    await versionManager.AutoUpdateAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "應用程式發生未處理的例外狀況");
    Environment.ExitCode = 1;
}
finally
{
    Log.CloseAndFlush();
}