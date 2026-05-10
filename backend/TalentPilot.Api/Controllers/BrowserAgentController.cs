using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentPilot.Api.Services.BrowserAgent;
using TalentPilot.Api.Services;

namespace TalentPilot.Api.Controllers;

/// <summary>
/// Browser Agent 控制器 - 浏览器自动化简历采集 API（按需采集）
/// </summary>
[ApiController]
[Route("api/browser-agent")]
[Authorize(Roles = "admin")]
public class BrowserAgentController : ControllerBase
{
    private readonly BossPlatform _bossPlatform;
    private readonly PlaywrightBrowserManager _browserManager;
    private readonly CookieSessionManager _cookieManager;
    private readonly IJobPostService _jobPostService;
    private readonly ILogger<BrowserAgentController> _logger;

    public BrowserAgentController(
        BossPlatform bossPlatform,
        PlaywrightBrowserManager browserManager,
        CookieSessionManager cookieManager,
        IJobPostService jobPostService,
        ILogger<BrowserAgentController> logger)
    {
        _bossPlatform = bossPlatform;
        _browserManager = browserManager;
        _cookieManager = cookieManager;
        _jobPostService = jobPostService;
        _logger = logger;
    }

    /// <summary>
    /// 按需采集：针对指定职位从 Boss 搜索并采集符合要求的简历
    /// </summary>
    /// <param name="request">必须传入 jobPostId</param>
    [HttpPost("collect")]
    public async Task<ActionResult<ApiResponse<object>>> Collect(
        [FromBody] BrowserAgentCollectRequest request,
        CancellationToken ct = default)
    {
        if (request.JobPostId == null || request.JobPostId <= 0)
        {
            return BadRequest(new ApiResponse<object>(false, "jobPostId 是必填参数，用于指定采集目标职位", null));
        }

        _logger.LogInformation("收到按需采集请求: jobPostId={JobPostId}", request.JobPostId);

        var result = await _bossPlatform.RunCollectionAsync(request.JobPostId.Value, ct);

        var statusCode = result.Status switch
        {
            "Completed" => 200,
            "NeedsManualLogin" => 401,
            "Failed" => 500,
            "Cancelled" => 499,
            _ => 200
        };

        return StatusCode(statusCode, new ApiResponse<object>(
            result.Status == "Completed",
            result.Message ?? result.Status,
            result));
    }

    /// <summary>
    /// 获取职位列表（供 HR 选择要采集哪个职位）
    /// </summary>
    [HttpGet("job-posts")]
    public async Task<ActionResult<ApiResponse<object>>> GetJobPosts(
        [FromQuery] string? status = "open",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var (items, total) = await _jobPostService.GetAllAsync(status, page, pageSize);

        return Ok(new ApiResponse<object>(true, "获取成功", new
        {
            total,
            page,
            pageSize,
            items = items.Select(j => new
            {
                j.Id,
                j.Title,
                j.Status,
                j.CreatedAt,
                j.Requirements
            })
        }));
    }

    /// <summary>
    /// 保存当前登录态（手动登录后调用）
    /// </summary>
    [HttpPost("save-session")]
    public async Task<ActionResult<ApiResponse<object>>> SaveSession(
        [FromBody] SaveSessionRequest request)
    {
        var platform = request.Platform ?? "boss";
        await _bossPlatform.SaveCurrentSessionAsync();
        return Ok(new ApiResponse<object>(true, $"已保存 {platform} 登录态", null));
    }

    /// <summary>
    /// 检查登录态
    /// </summary>
    [HttpGet("login-status")]
    public async Task<ActionResult<ApiResponse<object>>> CheckLoginStatus(
        [FromQuery] string platform = "boss")
    {
        var hasCookies = _cookieManager.HasCookies(platform);
        var isValid = await _cookieManager.ValidateCookiesAsync(platform);

        return Ok(new ApiResponse<object>(true, "查询成功", new
        {
            platform,
            hasCookies,
            isValid,
            status = !hasCookies ? "NoCookies" : isValid ? "Valid" : "Expired",
            nextStep = !hasCookies ? "请先手动登录并调用 /api/browser-agent/save-session" :
                       !isValid ? "Cookies 已过期，请重新登录并保存" :
                       "登录态有效，可以开始采集"
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
            endpoint = "http://localhost:9222",
            hint = !connected ? "请在 Windows CMD 中运行: chrome.exe --remote-debugging-port=9222 --user-data-dir=\"C:\\Users\\<USER>\\chrome-debug\"" : null
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
    /// <summary>
    /// 必填：目标职位 ID（从 /api/browser-agent/job-posts 获取列表）
    /// </summary>
    public int? JobPostId { get; set; }
}

public class SaveSessionRequest
{
    public string Platform { get; set; } = "boss";
}

public class NavigateRequest
{
    public string Url { get; set; } = "";
}
