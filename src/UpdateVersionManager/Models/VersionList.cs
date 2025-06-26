namespace UpdateVersionManager.Models;

public class VersionInfo
{
    public string Version { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string? Sha256 { get; set; }
    public long Size { get; set; }
    public string ReleaseDate { get; set; } = "";
    public string Description { get; set; } = "";
}

public class VersionList
{
    public List<VersionInfo> Versions { get; set; } = new();
}