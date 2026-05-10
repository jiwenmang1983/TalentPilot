using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentPilot.Api.Services.BrowserAgent;

namespace TalentPilot.Api.Controllers;

/// <summary>
/// Browser Agent 控制器 - 浏览器自动化简历采集 API
/// </summary>
[ApiController]
[Route("api/browser-agent")]
[Authorize(Roles = "admin")]
public class BrowserAgentController : ControllerBase
{
    private readonly BossPlatform _bossPlatform;
    private readonly PlaywrightBrowserManager _browserManager;
    private readonly CookieSessionManager _cookieManager;
    private readonly ILogger<BrowserAgentController> _logger;

    public BrowserAgentController(
        BossPlatform bossPlatform,
        PlaywrightBrowserManager browserManager,
        CookieSessionManager cookieManager,
        ILogger<BrowserAgentController> logger)
    {
        _bossPlatform = bossPlatform;
        _browserManager = browserManager;
        _cookieManager = cookieManager;
        _logger = logger;
    }

    /// <summary>
    /// 触发简历采集
    /// </summary>
    [HttpPost("collect")]
    public async Task<ActionResult<ApiResponse<object>>> Collect([FromBody] BrowserAgentCollectRequest? request = null)
    {
        var platform = request?.Platform ?? "boss";
        var maxCandidates = request?.MaxCandidates ?? 10;

        _logger.LogInformation("收到采集请求: platform={Platform}, maxCandidates={Max}", platform, maxCandidates);

        if (platform != "boss")
            return BadRequest(new ApiResponse<object>(false, "目前仅支持 boss 平台", null));

        var result = await _bossPlatform.RunCollectionAsync(maxCandidates);

        return Ok(new ApiResponse<object>(true, result.Message ?? "采集完成", result));
    }

    /// <summary>
    /// 保存当前登录态（手动登录后调用）
    /// </summary>
    [HttpPost("save-session")]
    public async Task<ActionResult<ApiResponse<object>>> SaveSession([FromBody] SaveSessionRequest request)
    {
        var platform = request.Platform ?? "boss";
        await _bossPlatform.SaveCurrentSessionAsync();
        return Ok(new ApiResponse<object>(true, $"已保存 {platform} 登录态", null));
    }

    /// <summary>
    /// 检查登录态
    /// </summary>
    [HttpGet("login-status")]
    public async Task<ActionResult<ApiResponse<object>>> CheckLoginStatus([FromQuery] string platform = "boss")
    {
        var hasCookies = _cookieManager.HasCookies(platform);
        var isValid = await _cookieManager.ValidateCookiesAsync(platform);

        return Ok(new ApiResponse<object>(true, "查询成功", new
        {
            platform,
            hasCookies,
            isValid,
            status = !hasCookies ? "NoCookies" : isValid ? "Valid" : "Expired"
        }));
    }

    /// <summary>
    /// 删除登录态
    /// </summary>
    [HttpDelete("session")]
    public ActionResult<ApiResponse<object>> DeleteSession([FromQuery] string platform = "boss")
    {
        _cookieManager.DeleteCookies(platform);
        return Ok(new ApiResponse<object>(true, $"已删除 {platform} 登录态", null));
    }

    /// <summary>
    /// 连接状态检查
    /// </summary>
    [HttpGet("connection")]
    public async Task<ActionResult<ApiResponse<object>>> CheckConnection()
    {
        var connected = await _browserManager.ConnectAsync();
        return Ok(new ApiResponse<object>(true, connected ? "已连接到浏览器" : "连接失败", new
        {
            connected,
            endpoint = "http://localhost:9222"
        }));
    }

    /// <summary>
    /// 手动导航到 URL（调试用）
    /// </summary>
    [HttpPost("navigate")]
    public async Task<ActionResult<ApiResponse<object>>> Navigate([FromBody] NavigateRequest request)
    {
        if (!_browserManager.IsConnected)
        {
            var connected = await _browserManager.ConnectAsync();
            if (!connected)
                return BadRequest(new ApiResponse<object>(false, "无法连接到浏览器，请确认 Chrome Debug 模式已启动", null));
        }

        var success = await _browserManager.NavigateAsync(request.Url);
        return Ok(new ApiResponse<object>(success, success ? "导航成功" : "导航失败", new { url = request.Url }));
    }

    /// <summary>
    /// 截图当前页面（调试用）
    /// </summary>
    [HttpPost("screenshot")]
    public async Task<ActionResult<ApiResponse<object>>> Screenshot()
    {
        var screenshot = await _browserManager.ScreenshotCurrentPageAsync();
        if (screenshot == null)
            return BadRequest(new ApiResponse<object>(false, "截图失败", null));

        var base64 = Convert.ToBase64String(screenshot);
        return Ok(new ApiResponse<object>(true, "截图成功", new { size = screenshot.Length, base64 }));
    }
}

public class BrowserAgentCollectRequest
{
    public string Platform { get; set; } = "boss";
    public int MaxCandidates { get; set; } = 10;
}

public class SaveSessionRequest
{
    public string Platform { get; set; } = "boss";
}

public class NavigateRequest
{
    public string Url { get; set; } = "";
}
