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

// 解析命令行參數以檢查是否有指定設定檔
var commandLineArgs = Environment.GetCommandLineArgs();
var configPath = GetConfigPath(commandLineArgs);

// 建立設定
var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
var configuration = BuildConfiguration(environment, configPath);

// 輔助方法：過濾設定檔參數
static string[] FilterConfigArgs(string[] args)
{
    var filteredArgs = new List<string>();
    
    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] == "--config" || args[i] == "-c")
        {
            // 跳過 --config 參數和它的值
            i++; // 跳過路徑參數
            continue;
        }
        
        filteredArgs.Add(args[i]);
    }
    
    return filteredArgs.ToArray();
}

// 輔助方法：取得設定檔路徑
static string? GetConfigPath(string[] args)
{
    // 1. 檢查命令行參數 --config
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (args[i] == "--config" || args[i] == "-c")
        {
            return args[i + 1];
        }
    }
    
    // 2. 檢查環境變數 UVM_CONFIG
    var envConfig = Environment.GetEnvironmentVariable("UVM_CONFIG");
    if (!string.IsNullOrEmpty(envConfig) && File.Exists(envConfig))
    {
        return envConfig;
    }
    
    // 3. 檢查當前工作目錄下的設定檔（依優先順序）
    var currentDir = Directory.GetCurrentDirectory();
    var configFiles = new[] { "uvm.json", "appsettings.json", "versions.json" };
    
    foreach (var configFile in configFiles)
    {
        var path = Path.Combine(currentDir, configFile);
        if (File.Exists(path))
        {
            return path;
        }
    }
    
    return null;
}

// 輔助方法：建立設定
static IConfiguration BuildConfiguration(string environment, string? customConfigPath = null)
{
    var builder = new ConfigurationBuilder();
    
    // 如果指定了自定義設定檔路徑，優先使用
    if (!string.IsNullOrEmpty(customConfigPath) && File.Exists(customConfigPath))
    {
        Log.Information("使用自定義設定檔: {ConfigPath}", customConfigPath);
        
        // 如果是 versions.json，需要特殊處理
        if (Path.GetFileName(customConfigPath).Equals("versions.json", StringComparison.OrdinalIgnoreCase))
        {
            // 創建一個臨時的配置結構，包裝 versions.json
            var versionsJson = File.ReadAllText(customConfigPath);
            var tempConfig = $@"{{
  ""UpdateVersionManager"": {{
    ""VersionListSource"": ""{customConfigPath}"",
    ""LocalBaseDir"": ""app_versions"",
    ""CurrentVersionFile"": ""current_version.txt"",
    ""TempExtractPath"": ""temp_update"",
    ""ZipFilePath"": ""update.zip"",
    ""AppLinkName"": ""current"",
    ""VerboseOutput"": false
  }}
}}";
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(tempConfig)))
            {
                builder.AddJsonStream(stream);
            }
        }
        else
        {
            builder.AddJsonFile(customConfigPath, optional: false, reloadOnChange: true);
        }
        
        builder.AddEnvironmentVariables();
        return builder.Build();
    }
    
    // 預設行為：從應用程式目錄讀取設定
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
    services.AddSingleton<IUniversalDownloadService, UniversalDownloadService>();
    services.AddSingleton<ISymbolicLinkService, SymbolicLinkService>();
    services.AddSingleton<VersionManager>();
    services.AddSingleton<IOutputService, OutputService>();
    services.AddSingleton(provider => 
        provider.GetRequiredService<IOptions<UpdateVersionManagerSettings>>().Value);
    services.AddLogging(builder => builder.AddSerilog());
    
    // 傳遞當前使用的設定檔路徑
    if (!string.IsNullOrEmpty(configPath))
    {
        services.AddSingleton(new ConfigPathProvider(configPath));
    }

    var serviceProvider = services.BuildServiceProvider();

    // 解析命令行參數
    var versionManager = serviceProvider.GetRequiredService<VersionManager>();
    var output = serviceProvider.GetRequiredService<IOutputService>();

    if (commandLineArgs.Length > 1)
    {
        // 過濾掉設定檔參數，只傳遞實際的命令和參數給 CommandHandler
        var filteredArgs = FilterConfigArgs(commandLineArgs.Skip(1).ToArray());
        if (filteredArgs.Length > 0)
        {
            Log.Debug("執行命令: {Command} {Parameters}", filteredArgs[0], string.Join(" ", filteredArgs.Skip(1)));
            await CommandHandler.HandleCommand(filteredArgs[0], filteredArgs.Skip(1).ToArray(), versionManager, output);
            return;
        }
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