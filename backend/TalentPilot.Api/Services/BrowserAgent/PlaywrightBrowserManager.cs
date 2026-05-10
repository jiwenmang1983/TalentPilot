using Microsoft.Playwright;

namespace TalentPilot.Api.Services.BrowserAgent;

/// <summary>
/// Playwright 浏览器管理器 - 通过 CDP 连接到 Windows Chrome
/// 注意：WSL 中通过 --remote-debugging-port=9222 连接到 Windows Chrome
/// </summary>
public class PlaywrightBrowserManager : IAsyncDisposable
{
    private readonly ILogger<PlaywrightBrowserManager> _logger;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;
    private readonly string _cdpEndpoint;
    private bool _isConnected;
    private IPlaywright? _playwright;

    public bool IsConnected => _isConnected;
    public IPage? CurrentPage => _page;

    public PlaywrightBrowserManager(ILogger<PlaywrightBrowserManager> logger, IConfiguration configuration)
    {
        _logger = logger;
        _cdpEndpoint = configuration["BrowserAgent:CdpEndpoint"] ?? "http://localhost:9222";
    }

    /// <summary>
    /// 连接到 Windows Chrome CDP
    /// </summary>
    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("尝试连接到 Chrome CDP: {Endpoint}", _cdpEndpoint);

            _playwright = await Playwright.CreateAsync();

            var wsEndpoint = _cdpEndpoint.Replace("http://", "ws://").Replace("https://", "wss://");
            if (!wsEndpoint.StartsWith("ws://") && !wsEndpoint.StartsWith("wss://"))
                wsEndpoint = "ws://" + wsEndpoint;

            try
            {
                _browser = await _playwright.Chromium.ConnectAsync(wsEndpoint);
            }
            catch
            {
                _logger.LogWarning("CDP 连接失败，尝试启动本地浏览器（仅开发测试用）");
                _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = true,
                    Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
                });
            }

            if (_browser == null)
            {
                _logger.LogError("无法连接到浏览器");
                return false;
            }

            _context = await _browser.NewContextAsync();
            _page = await _context.NewPageAsync();
            _isConnected = true;

            _logger.LogInformation("成功连接到浏览器");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "连接 Chrome CDP 失败: {Endpoint}", _cdpEndpoint);
            _isConnected = false;
            return false;
        }
    }

    /// <summary>
    /// 截图当前页面
    /// </summary>
    public async Task<byte[]?> ScreenshotCurrentPageAsync(string? selector = null, bool fullPage = false)
    {
        if (_page == null)
        {
            _logger.LogWarning("页面未初始化");
            return null;
        }

        try
        {
            if (!string.IsNullOrEmpty(selector))
            {
                var element = _page.Locator(selector).First;
                await element.ScrollIntoViewIfNeededAsync();
                return await element.ScreenshotAsync();
            }

            return await _page.ScreenshotAsync(new PageScreenshotOptions { FullPage = fullPage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "截图失败");
            return null;
        }
    }

    /// <summary>
    /// 导航到 URL
    /// </summary>
    public async Task<bool> NavigateAsync(string url, int waitForSelectorTimeout = 15000)
    {
        if (_page == null) return false;

        try
        {
            await _page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.Load,
                Timeout = waitForSelectorTimeout
            });
            
            await Task.Delay(Random.Shared.Next(1000, 2000));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导航失败: {Url}", url);
            return false;
        }
    }

    /// <summary>
    /// 点击元素
    /// </summary>
    public async Task<bool> ClickAsync(string selector, int timeout = 5000)
    {
        if (_page == null) return false;
        try
        {
            await _page.Locator(selector).First.ClickAsync(new LocatorClickOptions { Timeout = timeout });
            await Task.Delay(Random.Shared.Next(500, 1000));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "点击失败: {Selector}", selector);
            return false;
        }
    }

    /// <summary>
    /// 滚动页面
    /// </summary>
    public async Task ScrollDownAsync(int pixels = 500)
    {
        if (_page == null) return;
        await _page.EvaluateAsync($"window.scrollBy(0, {pixels})");
        await Task.Delay(Random.Shared.Next(300, 800));
    }

    /// <summary>
    /// 等待元素出现
    /// </summary>
    public async Task<bool> WaitForSelectorAsync(string selector, int timeout = 10000)
    {
        if (_page == null) return false;
        try
        {
            await _page.Locator(selector).First.WaitForAsync(new LocatorWaitForOptions { Timeout = timeout });
            return true;
        }
        catch { return false; }
    }

    /// <summary>
    /// 检查是否需要登录（通过页面内容判断）
    /// </summary>
    public bool NeedsLogin()
    {
        if (_page == null) return true;
        var url = _page.Url;
        return url.Contains("login") || url.Contains("zhipin.com/web_geek");
    }

    public async ValueTask DisposeAsync()
    {
        if (_page != null) await _page.CloseAsync();
        if (_context != null) await _context.CloseAsync();
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
        _isConnected = false;
    }
}