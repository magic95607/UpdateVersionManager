namespace UpdateVersionManager.Models;

public class UpdateVersionManagerSettings
{
    public string GoogleDriveVersionListFileId { get; set; } = string.Empty;
    public string LocalBaseDir { get; set; } = string.Empty;
    public string CurrentVersionFile { get; set; } = string.Empty;
    public string TempExtractPath { get; set; } = string.Empty;
    public string ZipFilePath { get; set; } = string.Empty;
    public string AppLinkName { get; set; } = string.Empty;

    public string VersionListUrl => $"https://drive.google.com/uc?export=download&id={GoogleDriveVersionListFileId}";
}
