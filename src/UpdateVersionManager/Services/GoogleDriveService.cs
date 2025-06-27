using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace UpdateVersionManager.Services;

public class GoogleDriveService : IGoogleDriveService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GoogleDriveService> _logger;

    public GoogleDriveService(ILogger<GoogleDriveService> logger)
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "AutoUpdater/1.0");
        _logger = logger;
    }

    public async Task<string> DownloadTextAsync(string url)
    {
        try
        {
            Console.WriteLine($"正在連線到: {url}");
            var response = await _httpClient.GetAsync(url);

            Console.WriteLine($"HTTP 狀態碼: {response.StatusCode}");

            // 檢查是否成功
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"HTTP 請求失敗: {response.StatusCode} {response.ReasonPhrase}");
            }

            // Google Drive 可能會重定向到病毒掃描警告頁面
            if (response.Headers.Location != null)
            {
                Console.WriteLine($"重定向到: {response.Headers.Location}");
                response = await _httpClient.GetAsync(response.Headers.Location);
            }

            var content = await response.Content.ReadAsStringAsync();

            // 檢查是否為 Google Drive 錯誤頁面
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
                throw new Exception("取得的是 HTML 頁面而非 JSON 檔案，請檢查 Google Drive 檔案 ID 和分享設定");
            }

            return content;
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"網路請求失敗: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            throw new Exception($"請求逾時: {ex.Message}");
        }
    }

    public async Task DownloadFileAsync(string url, string filePath)
    {
        _logger.LogInformation("開始下載檔案，URL: {Url}，目標路徑: {FilePath}", url, filePath);
        
        try
        {
            var response = await _httpClient.GetAsync(url);
            _logger.LogInformation("下載回應狀態: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"HTTP 請求失敗: {response.StatusCode} {response.ReasonPhrase}");
            }

            // 先讀取回應內容來檢查是否為 HTML 頁面
            var contentBytes = await response.Content.ReadAsByteArrayAsync();
            var contentType = response.Content.Headers.ContentType?.MediaType;
            _logger.LogInformation("內容類型: {ContentType}，內容大小: {Size} bytes", contentType, contentBytes.Length);

            // 檢查是否為 HTML 內容（可能是 Google Drive 的確認頁面）
            var isHtmlContent = contentType?.Contains("text/html") == true || 
                               (contentBytes.Length > 0 && System.Text.Encoding.UTF8.GetString(contentBytes, 0, Math.Min(100, contentBytes.Length)).TrimStart().StartsWith("<"));

            if (isHtmlContent)
            {
                var htmlContent = System.Text.Encoding.UTF8.GetString(contentBytes);
                _logger.LogWarning("收到 HTML 內容而非檔案，內容長度: {Length}", htmlContent.Length);

                // 檢查是否為病毒掃描確認頁面
                if (htmlContent.Contains("Google Drive - Virus scan warning") || htmlContent.Contains("virus-scan-warning"))
                {
                    _logger.LogInformation("偵測到 Google Drive 病毒掃描警告頁面，嘗試取得確認連結");
                    
                    // 尋找確認連結的多種模式
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
                        response = await _httpClient.GetAsync(confirmUrl);
                        contentBytes = await response.Content.ReadAsByteArrayAsync();
                        _logger.LogInformation("確認下載回應狀態: {StatusCode}，內容大小: {Size} bytes", response.StatusCode, contentBytes.Length);
                        
                        // 再次檢查是否還是 HTML
                        var newContentType = response.Content.Headers.ContentType?.MediaType;
                        if (newContentType?.Contains("text/html") == true || 
                            System.Text.Encoding.UTF8.GetString(contentBytes, 0, Math.Min(100, contentBytes.Length)).TrimStart().StartsWith("<"))
                        {
                            _logger.LogError("確認後仍收到 HTML 內容，可能是檔案太大或需要特殊權限");
                            throw new Exception("Google Drive 檔案下載失敗：可能是檔案太大、需要權限或分享設定不正確");
                        }
                    }
                    else
                    {
                        _logger.LogError("無法找到確認連結");
                        throw new Exception("Google Drive 病毒掃描頁面：無法找到確認下載連結");
                    }
                }
                else
                {
                    _logger.LogError("收到非預期的 HTML 內容");
                    
                    // 記錄前 500 字符以供調試
                    var preview = htmlContent.Length > 500 ? htmlContent.Substring(0, 500) : htmlContent;
                    _logger.LogDebug("HTML 內容預覽: {Preview}", preview);
                    
                    throw new Exception("下載失敗：收到 HTML 頁面而非檔案，請檢查 Google Drive 分享連結設定");
                }
            }

            // 檢查檔案大小是否合理
            if (contentBytes.Length < 1000) // 小於 1KB 可能有問題
            {
                _logger.LogWarning("下載的檔案很小 ({Size} bytes)，可能不是預期的檔案", contentBytes.Length);
            }

            // 將檔案寫入磁碟
            await File.WriteAllBytesAsync(filePath, contentBytes);
            
            var fileInfo = new FileInfo(filePath);
            _logger.LogInformation("檔案下載完成，最終大小: {Size} bytes", fileInfo.Length);
            
            // 驗證下載的檔案是否為有效的 ZIP 檔案（如果檔案名是 .zip）
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
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "網路請求失敗");
            throw new Exception($"網路請求失敗: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "請求逾時");
            throw new Exception($"請求逾時: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "檔案下載失敗");
            throw;
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}