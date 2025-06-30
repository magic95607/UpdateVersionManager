namespace UpdateVersionManager.Models;

public class ConfigPathProvider
{
    public string? ConfigPath { get; }

    public ConfigPathProvider(string? configPath)
    {
        ConfigPath = configPath;
    }
}
