using System.Net.Http;
using System.Text.RegularExpressions;

namespace UpdateVersionManager.Services;

public class GoogleDriveService : IDisposable
{
    private readonly HttpClient _httpClient;

    public GoogleDriveService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "AutoUpdater/1.0");
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
        var response = await _httpClient.GetAsync(url);

        // 處理 Google Drive 的重定向和確認頁面
        var content = await response.Content.ReadAsStringAsync();

        // 檢查是否為病毒掃描確認頁面
        if (content.Contains("Google Drive - Virus scan warning"))
        {
            var confirmMatch = Regex.Match(content, @"href=""([^""]*&confirm=[^""]*)""");
            if (confirmMatch.Success)
            {
                var confirmUrl = confirmMatch.Groups[1].Value.Replace("&amp;", "&");
                response = await _httpClient.GetAsync($"https://drive.google.com{confirmUrl}");
            }
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = File.Create(filePath);
        await stream.CopyToAsync(fileStream);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}