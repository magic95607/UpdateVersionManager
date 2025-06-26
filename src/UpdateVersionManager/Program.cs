using UpdateVersionManager;
using UpdateVersionManager.Services;
using UpdateVersionManager.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

// 建立設定
var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

// 設定服務
var services = new ServiceCollection();
services.Configure<UpdateVersionManagerSettings>(
    configuration.GetSection("UpdateVersionManager"));
services.AddSingleton<FileService>();
services.AddSingleton<GoogleDriveService>();
services.AddSingleton<SymbolicLinkService>();
services.AddSingleton<VersionManager>();

var serviceProvider = services.BuildServiceProvider();

// 解析命令行參數
var commandLineArgs = Environment.GetCommandLineArgs();
var versionManager = serviceProvider.GetRequiredService<VersionManager>();

if (commandLineArgs.Length > 1)
{
    await CommandHandler.HandleCommand(commandLineArgs[1], commandLineArgs.Skip(2).ToArray(), versionManager);
    return;
}

// 預設行為：自動更新
await versionManager.AutoUpdateAsync();