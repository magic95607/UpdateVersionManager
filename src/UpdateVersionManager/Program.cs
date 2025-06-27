using UpdateVersionManager;
using UpdateVersionManager.Services;
using UpdateVersionManager.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using System.Text;

// 設定 Console 編碼為 UTF-8
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

// 建立設定
var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
var basePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? Directory.GetCurrentDirectory();
var configuration = new ConfigurationBuilder()
    .SetBasePath(basePath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

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