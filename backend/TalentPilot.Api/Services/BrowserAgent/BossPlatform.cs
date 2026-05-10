using Microsoft.Playwright;

namespace TalentPilot.Api.Services.BrowserAgent;

/// <summary>
/// Boss 直聘平台采集适配器
/// 实现：登录 → 列表页 → 截图识别 → 详情页 → 截图识别 → 存入DB
/// </summary>
public class BossPlatform
{
    private readonly PlaywrightBrowserManager _browser;
    private readonly VisionParser _visionParser;
    private readonly CookieSessionManager _cookieManager;
    private readonly ILogger<BossPlatform> _logger;

    public BossPlatform(
        PlaywrightBrowserManager browser,
        VisionParser visionParser,
        CookieSessionManager cookieManager,
        ILogger<BossPlatform> logger)
    {
        _browser = browser;
        _visionParser = visionParser;
        _cookieManager = cookieManager;
        _logger = logger;
    }

    /// <summary>
    /// 确保已登录（加载 cookies 或提示手动登录）
    /// </summary>
    public async Task<bool> EnsureLoggedInAsync()
    {
        if (!_browser.IsConnected)
        {
            var connected = await _browser.ConnectAsync();
            if (!connected) return false;
        }

        if (_cookieManager.HasCookies("boss"))
        {
            _logger.LogInformation("检测到已保存的 Boss cookies，尝试恢复登录态");
            await _browser.NavigateAsync("https://www.zhipin.com/webchat/");
            await Task.Delay(2000);
            
            if (_browser.NeedsLogin())
            {
                _logger.LogWarning("Cookies 已过期或无效，需要重新手动登录");
                return false;
            }
            return true;
        }

        _logger.LogWarning("未检测到 Boss cookies，请在 Chrome Debug 窗口中手动登录一次");
        await _browser.NavigateAsync("https://www.zhipin.com/webchat/");
        return false;
    }

    /// <summary>
    /// 保存当前浏览器 cookies
    /// </summary>
    public async Task SaveCurrentSessionAsync()
    {
        if (_browser.CurrentPage == null) return;

        try
        {
            var cookies = await _browser.CurrentPage.Context.CookiesAsync(
                new[] { "https://www.zhipin.com" });
            
            var cookieDataList = cookies.Select(c => new CookieData
            {
                Name = c.Name,
                Value = c.Value,
                Domain = c.Domain,
                Path = c.Path,
                Expires = c.Expires > 0 ? DateTimeOffset.FromUnixTimeSeconds((long)c.Expires).UtcDateTime : null,
                HttpOnly = c.HttpOnly,
                Secure = c.Secure
            }).ToList();

            await _cookieManager.SaveCookiesAsync("boss", cookieDataList);
            _logger.LogInformation("Boss cookies 保存成功，共 {Count} 条", cookieDataList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存 Boss cookies 失败");
        }
    }

    /// <summary>
    /// 采集简历列表
    /// </summary>
    public async Task<List<ResumeListItem>> CollectResumeListAsync(int maxPages = 5)
    {
        var allItems = new List<ResumeListItem>();

        await _browser.NavigateAsync("https://www.zhipin.com/webchat/");
        await Task.Delay(2000);

        for (int page = 0; page < maxPages; page++)
        {
            _logger.LogInformation("采集列表页 {Page}", page + 1);

            await _browser.WaitForSelectorAsync(".job-list-box", 10000);
            
            var screenshot = await _browser.ScreenshotCurrentPageAsync(fullPage: false);
            if (screenshot == null)
            {
                _logger.LogWarning("截图失败，跳过第 {Page} 页", page + 1);
                continue;
            }

            var items = await _visionParser.ParseResumeListAsync(screenshot);
            _logger.LogInformation("第 {Page} 页识别到 {Count} 个候选人", page + 1, items.Count);
            allItems.AddRange(items);

            await _browser.ScrollDownAsync(500);
            await Task.Delay(1500);

            var hasNext = await _browser.ClickAsync(".next-btn:not(.disabled)", 3000);
            if (!hasNext)
            {
                _logger.LogInformation("已到最后一页");
                break;
            }

            await Task.Delay(2000);
        }

        return allItems;
    }

    /// <summary>
    /// 采集简历详情
    /// </summary>
    public async Task<ResumeScreenshotResult?> CollectResumeDetailAsync(string detailPageUrl)
    {
        await _browser.NavigateAsync(detailPageUrl, 20000);
        await Task.Delay(2000);

        await _browser.WaitForSelectorAsync(".resume-detail, .candidate-info", 10000);

        var screenshot = await _browser.ScreenshotCurrentPageAsync(fullPage: true);
        if (screenshot == null) return null;

        await _browser.ScrollDownAsync(800);
        await Task.Delay(1000);
        var screenshot2 = await _browser.ScreenshotCurrentPageAsync(fullPage: true);

        var result = await _visionParser.ParseResumeDetailAsync(screenshot);
        
        if (result != null && screenshot2 != null)
        {
            var result2 = await _visionParser.ParseResumeDetailAsync(screenshot2);
            if (result2 != null)
            {
                result.工作经历 ??= result2.工作经历;
                result.项目经历 ??= result2.项目经历;
            }
        }

        return result;
    }

    /// <summary>
    /// 完整的采集流程
    /// </summary>
    public async Task<BrowserAgentTaskResult> RunCollectionAsync(int maxCandidates = 10, CancellationToken ct = default)
    {
        var result = new BrowserAgentTaskResult
        {
            Platform = "boss",
            StartedAt = DateTime.UtcNow,
            Status = "Running"
        };

        try
        {
            var loggedIn = await EnsureLoggedInAsync();
            if (!loggedIn)
            {
                result.Status = "NeedsManualLogin";
                result.ErrorMessage = "请先在 Chrome Debug 窗口中手动登录 Boss 直聘，然后调用 /api/browser-agent/save-session 接口保存 cookies";
                return result;
            }

            var candidates = await CollectResumeListAsync(maxPages: 3);
            result.TotalCandidates = candidates.Count;

            _logger.LogInformation("共采集到 {Count} 个候选人，开始采集详情...", candidates.Count);

            result.CollectedCandidates = candidates;

            result.Status = "Completed";
            result.CompletedAt = DateTime.UtcNow;
            result.Message = $"成功采集 {candidates.Count} 个候选人";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Boss 采集失败");
            result.Status = "Failed";
            result.ErrorMessage = ex.Message;
        }

        return result;
    }
}

public class BrowserAgentTaskResult
{
    public string Platform { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalCandidates { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }
    public List<ResumeListItem>? CollectedCandidates { get; set; }
}