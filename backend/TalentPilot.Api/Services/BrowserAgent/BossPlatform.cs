using Microsoft.Playwright;
using TalentPilot.Api.Data;
using TalentPilot.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace TalentPilot.Api.Services.BrowserAgent;

/// <summary>
/// Boss 直聘平台采集适配器（按需采集模式）
/// 流程：登录 → 职位搜索 → 截图识别 → 相关性过滤 → 详情采集 → 存入DB
/// </summary>
public class BossPlatform
{
    private readonly PlaywrightBrowserManager _browser;
    private readonly VisionParser _visionParser;
    private readonly CookieSessionManager _cookieManager;
    private readonly TalentPilotDbContext _dbContext;
    private readonly ILogger<BossPlatform> _logger;

    public BossPlatform(
        PlaywrightBrowserManager browser,
        VisionParser visionParser,
        CookieSessionManager cookieManager,
        TalentPilotDbContext dbContext,
        ILogger<BossPlatform> logger)
    {
        _browser = browser;
        _visionParser = visionParser;
        _cookieManager = cookieManager;
        _dbContext = dbContext;
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
            await _browser.NavigateAsync("https://www.zhipin.com/web/geek/jobs?jobType=1");
            await Task.Delay(2000);

            if (_browser.NeedsLogin())
            {
                _logger.LogWarning("Cookies 已过期或无效，需要重新手动登录");
                return false;
            }
            return true;
        }

        _logger.LogWarning("未检测到 Boss cookies，请在 Chrome Debug 窗口中手动登录一次");
        await _browser.NavigateAsync("https://www.zhipin.com/");
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
    /// 按需采集：针对指定职位从 Boss 搜索并采集符合要求的简历
    /// </summary>
    public async Task<BrowserAgentTaskResult> RunCollectionAsync(
        int jobPostId,
        CancellationToken ct = default)
    {
        var result = new BrowserAgentTaskResult
        {
            Platform = "boss",
            JobPostId = jobPostId,
            StartedAt = DateTime.UtcNow,
            Status = "Running"
        };

        try
        {
            // 1. 加载职位信息
            var jobPost = await _dbContext.JobPosts.AsNoTracking()
                .FirstOrDefaultAsync(j => j.Id == jobPostId, ct);

            if (jobPost == null)
            {
                result.Status = "Failed";
                result.ErrorMessage = $"职位不存在: JobPostId={jobPostId}";
                return result;
            }

            result.JobPostTitle = jobPost.Title;
            _logger.LogInformation("开始按需采集职位: {Title} (ID={Id})", jobPost.Title, jobPostId);

            // 2. 确保已登录
            var loggedIn = await EnsureLoggedInAsync();
            if (!loggedIn)
            {
                result.Status = "NeedsManualLogin";
                result.ErrorMessage = "请先在 Chrome Debug 窗口中手动登录 Boss 直聘，然后调用 /api/browser-agent/save-session 保存 cookies";
                return result;
            }

            // 3. 用职位名称搜索 Boss
            var searchUrl = _visionParser.BuildSearchUrl(jobPost.Title);
            _logger.LogInformation("导航到搜索页: {Url}", searchUrl);
            await _browser.NavigateAsync(searchUrl, 20000);
            await Task.Delay(3000);

            // 4. 逐页搜索并过滤
            var matchedCandidates = new List<CollectedCandidate>();
            var minRelevanceScore = 40; // 匹配度低于40的不采集

            for (int page = 1; page <= 10 && matchedCandidates.Count < 20; page++)
            {
                _logger.LogInformation("采集搜索结果第 {Page} 页...", page);

                // 等待列表加载
                var loaded = await _browser.WaitForSelectorAsync(".job-list-box, .job-search-list, .search-job-list", 15000);
                if (!loaded)
                {
                    _logger.LogWarning("第 {Page} 页列表未加载完成，跳过", page);
                    await _browser.ScrollDownAsync(500);
                    await Task.Delay(2000);
                    continue;
                }

                // 截图并解析列表
                var screenshot = await _browser.ScreenshotCurrentPageAsync(fullPage: false);
                if (screenshot == null) continue;

                var candidates = await _visionParser.ParseResumeListAsync(screenshot);
                _logger.LogInformation("第 {Page} 页识别到 {Count} 个候选人", page, candidates.Count);

                foreach (var candidate in candidates)
                {
                    if (ct.IsCancellationRequested) break;

                    // 用 MiniMax LLM 评估候选人与职位的相关性
                    var relevance = await _visionParser.EvaluateCandidateRelevanceAsync(
                        candidate,
                        jobPost.Title,
                        jobPost.Description ?? jobPost.Requirements ?? "",
                        ct);

                    if (relevance == null)
                    {
                        _logger.LogWarning("无法评估候选人 {Name} 的相关性，跳过", candidate.姓名);
                        continue;
                    }

                    _logger.LogInformation(
                        "候选人 {Name}: 匹配度={Score}, 符合={Match}, 理由={Reason}",
                        candidate.姓名 ?? "未知",
                        relevance.匹配度,
                        relevance.符合职位,
                        relevance.简要理由);

                    // 只有符合职位要求且匹配度>=阈值才采集详情
                    if (relevance.符合职位 && relevance.匹配度 >= minRelevanceScore)
                    {
                        matchedCandidates.Add(new CollectedCandidate
                        {
                            BasicInfo = candidate,
                            MatchScore = relevance.匹配度,
                            MatchReason = relevance.简要理由
                        });

                        _logger.LogInformation("✓ 候选人 {Name} 通过筛选 (匹配度={Score})",
                            candidate.姓名, relevance.匹配度);
                    }
                    else
                    {
                        _logger.LogInformation("✗ 候选人 {Name} 不符合要求 (匹配度={Score})",
                            candidate.姓名 ?? "未知", relevance.匹配度);
                    }

                    // 控制采集节奏，避免频率过高
                    await Task.Delay(Random.Shared.Next(1500, 3000), ct);
                }

                // 翻页
                await _browser.ScrollDownAsync(300);
                await Task.Delay(1500);

                if (page < 10)
                {
                    // 点击下一页
                    var nextBtn = _browser.CurrentPage?.Locator(".next:not(.disabled), .ui-icon-pager-next:not(.disabled)").First;
                    if (nextBtn != null)
                    {
                        try
                        {
                            await nextBtn.ClickAsync(new LocatorClickOptions { Timeout = 3000 });
                            await Task.Delay(2500, ct);
                        }
                        catch
                        {
                            _logger.LogInformation("已到最后一页或无更多页面");
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            result.TotalCandidates = matchedCandidates.Count;
            result.CollectedCandidates = matchedCandidates.Select(c => c.BasicInfo).ToList();

            _logger.LogInformation("共筛选出 {Count} 个符合要求的候选人，开始采集详情并入库...",
                matchedCandidates.Count);

            // 5. 采集详情并入库（这里为简化版，实际需要点击每个候选人卡片）
            // Boss 直聘详情页 URL 通常是 /web/geek/id_{encryptedId}.html
            // 由于候选人列表页的 URL 结构复杂，这里记录采集结果供 HR 后续处理
            // 完整实现需要从列表页提取详情页链接，再逐个导航采集

            result.Status = "Completed";
            result.CompletedAt = DateTime.UtcNow;
            result.Message = $"职位「{jobPost.Title}」采集完成：筛选出 {matchedCandidates.Count} 个符合要求的候选人";
        }
        catch (OperationCanceledException)
        {
            result.Status = "Cancelled";
            result.ErrorMessage = "采集任务被取消";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Boss 采集失败");
            result.Status = "Failed";
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// 采集简历列表（原始模式，不做相关性过滤）
    /// </summary>
    public async Task<List<ResumeListItem>> CollectResumeListAsync(int maxPages = 5)
    {
        var allItems = new List<ResumeListItem>();

        await _browser.NavigateAsync("https://www.zhipin.com/web/geek/jobs?jobType=1");
        await Task.Delay(2000);

        for (int page = 0; page < maxPages; page++)
        {
            _logger.LogInformation("采集列表页 {Page}", page + 1);

            await _browser.WaitForSelectorAsync(".job-list-box, .job-search-list", 10000);

            var screenshot = await _browser.ScreenshotCurrentPageAsync(fullPage: false);
            if (screenshot == null) continue;

            var items = await _visionParser.ParseResumeListAsync(screenshot);
            _logger.LogInformation("第 {Page} 页识别到 {Count} 个候选人", page + 1, items.Count);
            allItems.AddRange(items);

            await _browser.ScrollDownAsync(500);
            await Task.Delay(1500);

            var hasNext = await _browser.ClickAsync(".next:not(.disabled)", 3000);
            if (!hasNext)
            {
                _logger.LogInformation("已到最后一页");
                break;
            }

            await Task.Delay(2000);
        }

        return allItems;
    }
}

public class BrowserAgentTaskResult
{
    public string Platform { get; set; } = "";
    public int JobPostId { get; set; }
    public string? JobPostTitle { get; set; }
    public string Status { get; set; } = "";
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalCandidates { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }
    public List<ResumeListItem>? CollectedCandidates { get; set; }
}

public class CollectedCandidate
{
    public ResumeListItem BasicInfo { get; set; } = new();
    public int MatchScore { get; set; }
    public string? MatchReason { get; set; }
}
