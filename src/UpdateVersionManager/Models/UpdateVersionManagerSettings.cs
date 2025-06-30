namespace UpdateVersionManager.Models;

public class UpdateVersionManagerSettings
{
    public string GoogleDriveVersionListFileId { get; set; } = string.Empty;
    public string VersionListUrl { get; set; } = string.Empty;
    public string VersionListSource { get; set; } = string.Empty; // 新增：本地版本清單檔案路徑
    public string LocalBaseDir { get; set; } = string.Empty;
    public string CurrentVersionFile { get; set; } = string.Empty;
    public string TempExtractPath { get; set; } = string.Empty;
    public string ZipFilePath { get; set; } = string.Empty;
    public string AppLinkName { get; set; } = string.Empty;
    public bool VerboseOutput { get; set; } = false;

    /// <summary>
    /// 取得版本清單的 URL 或路徑。依優先順序：VersionListSource > VersionListUrl > GoogleDriveVersionListFileId
    /// </summary>
    public string GetVersionListSource()
    {
        // 1. 優先使用本地版本清單檔案
        if (!string.IsNullOrEmpty(VersionListSource))
        {
            return VersionListSource;
        }
        
        // 2. 使用遠端 URL
        if (!string.IsNullOrEmpty(VersionListUrl))
        {
            return VersionListUrl;
        }
        
        // 3. 從 Google Drive ID 建構 URL
        if (!string.IsNullOrEmpty(GoogleDriveVersionListFileId))
        {
            return $"https://drive.google.com/uc?export=download&id={GoogleDriveVersionListFileId}";
        }
        
        throw new InvalidOperationException("必須設定 VersionListSource、VersionListUrl 或 GoogleDriveVersionListFileId");
    }

    /// <summary>
    /// 取得版本清單的 URL。優先使用 VersionListUrl，若為空則從 GoogleDriveVersionListFileId 建構 Google Drive URL
    /// </summary>
    [Obsolete("請使用 GetVersionListSource() 方法")]
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
