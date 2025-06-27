namespace UpdateVersionManager.Models;

public class UpdateVersionManagerSettings
{
    public string GoogleDriveVersionListFileId { get; set; } = string.Empty;
    public string VersionListUrl { get; set; } = string.Empty;
    public string LocalBaseDir { get; set; } = string.Empty;
    public string CurrentVersionFile { get; set; } = string.Empty;
    public string TempExtractPath { get; set; } = string.Empty;
    public string ZipFilePath { get; set; } = string.Empty;
    public string AppLinkName { get; set; } = string.Empty;
    public bool VerboseOutput { get; set; } = false;

    /// <summary>
    /// 取得版本清單的 URL。優先使用 VersionListUrl，若為空則從 GoogleDriveVersionListFileId 建構 Google Drive URL
    /// </summary>
    public string GetVersionListUrl()
    {
        if (!string.IsNullOrEmpty(VersionListUrl))
        {
            return VersionListUrl;
        }
        
        if (!string.IsNullOrEmpty(GoogleDriveVersionListFileId))
        {
            return $"https://drive.google.com/uc?export=download&id={GoogleDriveVersionListFileId}";
        }
        
        throw new InvalidOperationException("必須設定 VersionListUrl 或 GoogleDriveVersionListFileId");
    }
}
