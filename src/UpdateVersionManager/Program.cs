using UpdateVersionManager;
using UpdateVersionManager.Services;

// 解析命令行參數
var commandLineArgs = Environment.GetCommandLineArgs();
var versionManager = new VersionManager();

if (commandLineArgs.Length > 1)
{
    await CommandHandler.HandleCommand(commandLineArgs[1], commandLineArgs.Skip(2).ToArray(), versionManager);
    return;
}

// 預設行為：自動更新
await versionManager.AutoUpdateAsync();