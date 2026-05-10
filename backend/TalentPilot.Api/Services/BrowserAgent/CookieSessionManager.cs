using System.Text.Json;

namespace TalentPilot.Api.Services.BrowserAgent;

/// <summary>
/// Cookie/Session 持久化管理 - 保存和加载登录态
/// </summary>
public class CookieSessionManager
{
    private readonly ILogger<CookieSessionManager> _logger;
    private readonly string _sessionsFolder;
    private readonly string _chromeUserDataDir;

    public CookieSessionManager(ILogger<CookieSessionManager> logger, IConfiguration configuration)
    {
        _logger = logger;
        _sessionsFolder = Path.Combine(AppContext.BaseDirectory, "browser_sessions");
        _chromeUserDataDir = configuration["BrowserAgent:ChromeUserDataDir"] ?? @"C:\Users\<USER>\chrome-debug";
        
        Directory.CreateDirectory(_sessionsFolder);
    }

    /// <summary>
    /// 获取指定平台的 Cookie 文件路径
    /// </summary>
    private string GetCookieFilePath(string platform)
        => Path.Combine(_sessionsFolder, $"{platform}_cookies.json");

    /// <summary>
    /// 获取指定平台的 Session Storage 文件路径
    /// </summary>
    private string GetSessionFilePath(string platform)
        => Path.Combine(_sessionsFolder, $"{platform}_storage.json");

    /// <summary>
    /// 检查 Cookie 是否存在
    /// </summary>
    public bool HasCookies(string platform)
        => File.Exists(GetCookieFilePath(platform));

    /// <summary>
    /// 保存 cookies 到文件
    /// </summary>
    public async Task SaveCookiesAsync(string platform, List<CookieData> cookies)
    {
        var path = GetCookieFilePath(platform);
        var json = JsonSerializer.Serialize(cookies, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
        _logger.LogInformation("已保存 {Platform} cookies 到 {Path}, 共 {Count} 条", 
            platform, path, cookies.Count);
    }

    /// <summary>
    /// 加载 cookies
    /// </summary>
    public async Task<List<CookieData>?> LoadCookiesAsync(string platform)
    {
        var path = GetCookieFilePath(platform);
        if (!File.Exists(path)) return null;

        try
        {
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<List<CookieData>>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载 cookies 失败: {Path}", path);
            return null;
        }
    }

    /// <summary>
    /// 删除 cookies
    /// </summary>
    public void DeleteCookies(string platform)
    {
        var cookiePath = GetCookieFilePath(platform);
        var sessionPath = GetSessionFilePath(platform);
        
        if (File.Exists(cookiePath)) File.Delete(cookiePath);
        if (File.Exists(sessionPath)) File.Delete(sessionPath);
        
        _logger.LogInformation("已删除 {Platform} 的 cookies", platform);
    }

    /// <summary>
    /// 检查登录态是否有效
    /// </summary>
    public async Task<bool> ValidateCookiesAsync(string platform)
    {
        var cookies = await LoadCookiesAsync(platform);
        if (cookies == null || cookies.Count == 0) return false;

        // 检查 cookie 是否过期
        var now = DateTime.UtcNow;
        var hasValidCookie = cookies.Any(c => 
            !c.Expires.HasValue || c.Expires.Value > now);

        return hasValidCookie;
    }
}

public class CookieData
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
    public string? Domain { get; set; }
    public string? Path { get; set; }
    public DateTime? Expires { get; set; }
    public bool HttpOnly { get; set; }
    public bool Secure { get; set; }
}