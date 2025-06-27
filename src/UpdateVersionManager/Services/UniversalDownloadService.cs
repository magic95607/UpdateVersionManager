using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace UpdateVersionManager.Services;

public class UniversalDownloadService : IUniversalDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UniversalDownloadService> _logger;

    public UniversalDownloadService(ILogger<UniversalDownloadService> logger)
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "UpdateVersionManager/1.2");
        _logger = logger;
    }

    public async Task<string> DownloadTextAsync(string url)
    {
        _logger.LogInformation("開始下載文本內容，URL: {Url}", url);
        
        try
        {
            var sourceType = DetectUrlSource(url);
            _logger.LogDebug("偵測到 URL 來源類型: {SourceType}", sourceType);

            return sourceType switch
            {
                UrlSource.GoogleDrive => await DownloadGoogleDriveTextAsync(url),
                UrlSource.GitHub => await DownloadGitHubTextAsync(url),
                UrlSource.Ftp => await DownloadFtpTextAsync(url),
                _ => await DownloadHttpTextAsync(url)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下載文本內容失敗，URL: {Url}", url);
            throw new Exception($"下載文本內容失敗: {ex.Message}");
        }
    }

    public async Task DownloadFileAsync(string url, string filePath)
    {
        _logger.LogInformation("開始下載檔案，URL: {Url}，目標路徑: {FilePath}", url, filePath);
        
        try
        {
            var sourceType = DetectUrlSource(url);
            _logger.LogDebug("偵測到 URL 來源類型: {SourceType}", sourceType);

            switch (sourceType)
            {
                case UrlSource.GoogleDrive:
                    await DownloadGoogleDriveFileAsync(url, filePath);
                    break;
                case UrlSource.GitHub:
                    await DownloadGitHubFileAsync(url, filePath);
                    break;
                case UrlSource.Ftp:
                    await DownloadFtpFileAsync(url, filePath);
                    break;
                default:
                    await DownloadHttpFileAsync(url, filePath);
                    break;
            }

            await ValidateDownloadedFileAsync(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下載檔案失敗，URL: {Url}，目標路徑: {FilePath}", url, filePath);
            throw new Exception($"下載檔案失敗: {ex.Message}");
        }
    }

    private UrlSource DetectUrlSource(string url)
    {
        var uri = new Uri(url);
        
        if (uri.Host.Contains("drive.google.com") || uri.Host.Contains("docs.google.com"))
            return UrlSource.GoogleDrive;
            
        if (uri.Host.Contains("github.com") || uri.Host.Contains("githubusercontent.com"))
            return UrlSource.GitHub;
            
        if (uri.Scheme.Equals("ftp", StringComparison.OrdinalIgnoreCase) || 
            uri.Scheme.Equals("ftps", StringComparison.OrdinalIgnoreCase))
            return UrlSource.Ftp;
            
        return UrlSource.Http;
    }

    #region Google Drive 下載實作

    private async Task<string> DownloadGoogleDriveTextAsync(string url)
    {
        _logger.LogDebug("使用 Google Drive 下載文本，URL: {Url}", url);
        
        var response = await _httpClient.GetAsync(url);
        _logger.LogDebug("Google Drive 回應狀態: {StatusCode}", response.StatusCode);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"HTTP 請求失敗: {response.StatusCode} {response.ReasonPhrase}");
        }

        // 處理 Google Drive 重定向
        if (response.Headers.Location != null)
        {
            _logger.LogDebug("Google Drive 重定向到: {Location}", response.Headers.Location);
            response = await _httpClient.GetAsync(response.Headers.Location);
        }

        var content = await response.Content.ReadAsStringAsync();

        // 檢查常見的 Google Drive 錯誤
        if (content.Contains("Sorry, you can't view or download this file at this time."))
        {
            throw new Exception("Google Drive 檔案無法存取，請檢查分享設定");
        }

        if (content.Contains("Google Drive - Virus scan warning"))
        {
            throw new Exception("Google Drive 病毒掃描警告，請直接下載檔案或等待掃描完成");
        }

        // 檢查是否為 HTML 錯誤頁面
        if (content.TrimStart().StartsWith("<!DOCTYPE html>") || content.TrimStart().StartsWith("<html"))
        {
            throw new Exception("取得的是 HTML 頁面而非文本檔案，請檢查 Google Drive 檔案 ID 和分享設定");
        }

        return content;
    }

    private async Task DownloadGoogleDriveFileAsync(string url, string filePath)
    {
        _logger.LogDebug("使用 Google Drive 下載檔案，URL: {Url}", url);
        
        var response = await _httpClient.GetAsync(url);
        _logger.LogDebug("Google Drive 檔案下載回應狀態: {StatusCode}", response.StatusCode);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"HTTP 請求失敗: {response.StatusCode} {response.ReasonPhrase}");
        }

        var contentBytes = await response.Content.ReadAsByteArrayAsync();
        var contentType = response.Content.Headers.ContentType?.MediaType;
        _logger.LogDebug("內容類型: {ContentType}，內容大小: {Size} bytes", contentType, contentBytes.Length);

        // 檢查是否為 HTML 內容（可能是 Google Drive 的確認頁面）
        var isHtmlContent = contentType?.Contains("text/html") == true || 
                           (contentBytes.Length > 0 && System.Text.Encoding.UTF8.GetString(contentBytes, 0, Math.Min(100, contentBytes.Length)).TrimStart().StartsWith("<"));

        if (isHtmlContent)
        {
            contentBytes = await HandleGoogleDriveVirusScanAsync(contentBytes, url);
        }

        // 檢查檔案大小是否合理
        if (contentBytes.Length < 1000) // 小於 1KB 可能有問題
        {
            _logger.LogWarning("下載的檔案很小 ({Size} bytes)，可能不是預期的檔案", contentBytes.Length);
        }

        // 將檔案寫入磁碟
        await File.WriteAllBytesAsync(filePath, contentBytes);
        _logger.LogInformation("Google Drive 檔案下載完成，大小: {Size} bytes", contentBytes.Length);
    }

    private async Task<byte[]> HandleGoogleDriveVirusScanAsync(byte[] htmlBytes, string originalUrl)
    {
        var htmlContent = System.Text.Encoding.UTF8.GetString(htmlBytes);
        _logger.LogDebug("處理 Google Drive HTML 內容，長度: {Length}", htmlContent.Length);

        if (htmlContent.Contains("Google Drive - Virus scan warning") || htmlContent.Contains("virus-scan-warning"))
        {
            _logger.LogInformation("偵測到 Google Drive 病毒掃描警告頁面，嘗試取得確認連結");
            
            var confirmPatterns = new[]
            {
                @"href=""([^""]*&confirm=[^""]*)""",
                @"href=""([^""]*confirm=[^""]*)""",
                @"""([^""]*confirm=[^""]*&id=[^""]*)""",
                @"action=""([^""]*download[^""]*)"" method=""post"""
            };

            string? confirmUrl = null;
            foreach (var pattern in confirmPatterns)
            {
                var match = Regex.Match(htmlContent, pattern);
                if (match.Success)
                {
                    confirmUrl = match.Groups[1].Value.Replace("&amp;", "&");
                    if (!confirmUrl.StartsWith("http"))
                    {
                        confirmUrl = $"https://drive.google.com{confirmUrl}";
                    }
                    break;
                }
            }

            if (!string.IsNullOrEmpty(confirmUrl))
            {
                _logger.LogInformation("使用確認 URL: {ConfirmUrl}", confirmUrl);
                var response = await _httpClient.GetAsync(confirmUrl);
                var contentBytes = await response.Content.ReadAsByteArrayAsync();
                _logger.LogInformation("確認下載回應狀態: {StatusCode}，內容大小: {Size} bytes", response.StatusCode, contentBytes.Length);
                
                // 再次檢查是否還是 HTML
                var newContentType = response.Content.Headers.ContentType?.MediaType;
                if (newContentType?.Contains("text/html") == true || 
                    System.Text.Encoding.UTF8.GetString(contentBytes, 0, Math.Min(100, contentBytes.Length)).TrimStart().StartsWith("<"))
                {
                    throw new Exception("Google Drive 檔案下載失敗：可能是檔案太大或需要特殊權限");
                }

                return contentBytes;
            }
            else
            {
                throw new Exception("Google Drive 病毒掃描頁面：無法找到確認下載連結");
            }
        }
        else
        {
            throw new Exception("下載失敗：收到 HTML 頁面而非檔案，請檢查 Google Drive 分享連結設定");
        }
    }

    #endregion

    #region GitHub 下載實作

    private async Task<string> DownloadGitHubTextAsync(string url)
    {
        _logger.LogDebug("使用 GitHub 下載文本，URL: {Url}", url);
        
        // GitHub API 可能需要特殊處理
        var response = await _httpClient.GetAsync(url);
        
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new Exception("GitHub 檔案不存在或無法存取");
            }
            throw new HttpRequestException($"GitHub 請求失敗: {response.StatusCode} {response.ReasonPhrase}");
        }

        return await response.Content.ReadAsStringAsync();
    }

    private async Task DownloadGitHubFileAsync(string url, string filePath)
    {
        _logger.LogDebug("使用 GitHub 下載檔案，URL: {Url}", url);
        
        var response = await _httpClient.GetAsync(url);
        
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new Exception("GitHub 檔案不存在或無法存取");
            }
            throw new HttpRequestException($"GitHub 請求失敗: {response.StatusCode} {response.ReasonPhrase}");
        }

        var contentBytes = await response.Content.ReadAsByteArrayAsync();
        await File.WriteAllBytesAsync(filePath, contentBytes);
        
        _logger.LogInformation("GitHub 檔案下載完成，大小: {Size} bytes", contentBytes.Length);
    }

    #endregion

    #region FTP 下載實作

    private async Task<string> DownloadFtpTextAsync(string url)
    {
        _logger.LogDebug("使用 FTP 下載文本，URL: {Url}", url);
        
        try
        {
            // 使用 HttpClient 處理 FTP 下載，因為 WebRequest.Create 已過時
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"FTP 請求失敗: {response.StatusCode} {response.ReasonPhrase}");
            }

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("FTP 文本下載完成，大小: {Size} 字符", content.Length);
            
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FTP 文本下載失敗");
            throw new Exception($"FTP 下載失敗: {ex.Message}");
        }
    }

    private async Task DownloadFtpFileAsync(string url, string filePath)
    {
        _logger.LogDebug("使用 FTP 下載檔案，URL: {Url}", url);
        
        try
        {
            // 使用 HttpClient 處理 FTP 下載，因為 WebRequest.Create 已過時
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"FTP 請求失敗: {response.StatusCode} {response.ReasonPhrase}");
            }

            var contentBytes = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(filePath, contentBytes);
            
            var fileInfo = new FileInfo(filePath);
            _logger.LogInformation("FTP 檔案下載完成，大小: {Size} bytes", fileInfo.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FTP 檔案下載失敗");
            throw new Exception($"FTP 下載失敗: {ex.Message}");
        }
    }

    #endregion

    #region HTTP 下載實作

    private async Task<string> DownloadHttpTextAsync(string url)
    {
        _logger.LogDebug("使用 HTTP 下載文本，URL: {Url}", url);
        
        var response = await _httpClient.GetAsync(url);
        
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"HTTP 請求失敗: {response.StatusCode} {response.ReasonPhrase}");
        }

        return await response.Content.ReadAsStringAsync();
    }

    private async Task DownloadHttpFileAsync(string url, string filePath)
    {
        _logger.LogDebug("使用 HTTP 下載檔案，URL: {Url}", url);
        
        var response = await _httpClient.GetAsync(url);
        
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"HTTP 請求失敗: {response.StatusCode} {response.ReasonPhrase}");
        }

        var contentBytes = await response.Content.ReadAsByteArrayAsync();
        await File.WriteAllBytesAsync(filePath, contentBytes);
        
        _logger.LogInformation("HTTP 檔案下載完成，大小: {Size} bytes", contentBytes.Length);
    }

    #endregion

    #region 共用方法

    private async Task ValidateDownloadedFileAsync(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
        {
            throw new Exception("下載的檔案不存在");
        }

        _logger.LogInformation("檔案下載完成，最終大小: {Size} bytes", fileInfo.Length);

        // 如果是 ZIP 檔案，驗證檔案格式
        if (Path.GetExtension(filePath).ToLower() == ".zip" && fileInfo.Length > 0)
        {
            try
            {
                using var fileStream = File.OpenRead(filePath);
                var buffer = new byte[4];
                var bytesRead = await fileStream.ReadAsync(buffer.AsMemory(0, 4));
                
                // ZIP 檔案的魔術數字是 50 4B 03 04 (PK..)
                if (bytesRead >= 2 && (buffer[0] != 0x50 || buffer[1] != 0x4B))
                {
                    _logger.LogWarning("下載的檔案不是有效的 ZIP 格式");
                    throw new Exception("下載的檔案不是有效的 ZIP 格式，可能下載錯誤");
                }
                else if (bytesRead >= 2)
                {
                    _logger.LogInformation("已驗證檔案為有效的 ZIP 格式");
                }
            }
            catch (Exception ex) when (!(ex is Exception && ex.Message.Contains("ZIP 格式")))
            {
                _logger.LogWarning(ex, "無法驗證 ZIP 檔案格式");
            }
        }
    }

    #endregion

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

internal enum UrlSource
{
    GoogleDrive,
    GitHub,
    Ftp,
    Http
}
