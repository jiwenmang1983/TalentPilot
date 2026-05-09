using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TalentPilot.Api.Data;
using TalentPilot.Api.Models.Entities;
using TalentPilot.Api.Services;

namespace TalentPilot.Api.Services;

public class ContentAdaptationService
{
    private readonly IMiniMaxService _miniMaxService;
    private readonly TalentPilotDbContext _dbContext;
    private readonly ILogger<ContentAdaptationService> _logger;

    private static readonly Dictionary<string, string> ChannelNames = new()
    {
        { "boss", "Boss直聘" },
        { "liepin", "猎聘" },
        { "lagou", "拉勾" },
        { "linkedin", "领英" },
        { "xiaohongshu", "小红书" },
        { "custom", "自定义渠道" }
    };

    public ContentAdaptationService(
        IMiniMaxService miniMaxService,
        TalentPilotDbContext dbContext,
        ILogger<ContentAdaptationService> logger)
    {
        _miniMaxService = miniMaxService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<JobChannelContentDto>> AdaptJobPostAsync(int jobPostId, List<string> channelTypes, string createdBy)
    {
        var jobPost = await _dbContext.JobPosts.FindAsync(jobPostId);
        if (jobPost == null)
            throw new ArgumentException($"JobPost with id {jobPostId} not found");

        var results = new List<JobChannelContentDto>();

        foreach (var channelType in channelTypes)
        {
            try
            {
                var result = await AdaptToChannelAsync(jobPost, channelType, createdBy);
                results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to adapt job post {JobPostId} to channel {ChannelType}", jobPostId, channelType);
                var errorContent = new JobChannelContent
                {
                    JobPostId = jobPostId,
                    ChannelType = channelType,
                    ChannelName = ChannelNames.GetValueOrDefault(channelType, channelType),
                    Status = "failed",
                    ErrorMessage = ex.Message,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var existing = await _dbContext.JobChannelContents
                    .FirstOrDefaultAsync(c => c.JobPostId == jobPostId && c.ChannelType == channelType);

                if (existing != null)
                {
                    existing.Status = "failed";
                    existing.ErrorMessage = ex.Message;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    _dbContext.JobChannelContents.Add(errorContent);
                }

                await _dbContext.SaveChangesAsync();

                results.Add(MapToDto(errorContent));
            }
        }

        return results;
    }

    private async Task<JobChannelContentDto> AdaptToChannelAsync(JobPost jobPost, string channelType, string createdBy)
    {
        var prompt = GetChannelPrompt(channelType, jobPost);
        var response = await _miniMaxService.ChatAsync(prompt, 2048);

        // DEBUG: log response structure
        var contentItems = (response?.Content ?? new List<MiniMaxContentBlock>())
            .Select(c => $"type={c.Type}, textLen={c.Text?.Length ?? -1}")
            .ToList();
        _logger.LogInformation("MiniMax response: Id={Id}, ContentCount={Count}, Content=[{Items}]",
            response?.Id,
            response?.Content?.Count ?? 0,
            string.Join(" | ", contentItems));

        var existing = await _dbContext.JobChannelContents
            .FirstOrDefaultAsync(c => c.JobPostId == jobPost.Id && c.ChannelType == channelType);

        var content = new JobChannelContent
        {
            JobPostId = jobPost.Id,
            ChannelType = channelType,
            ChannelName = ChannelNames.GetValueOrDefault(channelType, channelType),
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (response == null || string.IsNullOrEmpty(response.GetFirstText()))
        {
            content.Status = "failed";
            content.ErrorMessage = "LLM returned empty response";
            content.AdaptationPrompt = prompt;
        }
        else
        {
            var llmText = response.GetFirstText()!;
            (content.AdaptedTitle, content.AdaptedContent, content.SkillTags) = ParseLlmResponse(llmText, channelType);
            content.WordCount = string.IsNullOrEmpty(content.AdaptedContent) ? 0 : content.AdaptedContent.Length;
            content.SalaryMin = (int?)jobPost.SalaryMin;
            content.SalaryMax = (int?)jobPost.SalaryMax;
            content.Status = "ready";
            content.AdaptationPrompt = prompt;
        }

        if (existing != null)
        {
            existing.AdaptedTitle = content.AdaptedTitle;
            existing.AdaptedContent = content.AdaptedContent;
            existing.WordCount = content.WordCount;
            existing.SkillTags = content.SkillTags;
            existing.SalaryMin = content.SalaryMin;
            existing.SalaryMax = content.SalaryMax;
            existing.Status = content.Status;
            existing.AdaptationPrompt = content.AdaptationPrompt;
            existing.ErrorMessage = content.ErrorMessage;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _dbContext.JobChannelContents.Add(content);
        }

        await _dbContext.SaveChangesAsync();
        return MapToDto(existing ?? content);
    }

    private (string? title, string? content, string? skillTags) ParseLlmResponse(string llmText, string channelType)
    {
        string? title = null;
        string? content = llmText;
        var skillTags = new List<string>();

        var lines = llmText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length > 0 && lines[0].StartsWith("#"))
        {
            title = lines[0].TrimStart('#', ' ').Trim();
        }
        else if (lines.Length > 0)
        {
            var titleMatch = System.Text.RegularExpressions.Regex.Match(lines[0], @"[\[【]?[^\]】]{2,30}[\]】]?");
            if (titleMatch.Success)
            {
                title = titleMatch.Value.Trim('[', ']', '【', '】');
            }
        }

        var skillsMatch = System.Text.RegularExpressions.Regex.Match(llmText, @"技能标签[：:]\s*([^\n]+(?:\n[^\n]+)*)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (skillsMatch.Success)
        {
            var skillsText = skillsMatch.Groups[1].Value;
            var matches = System.Text.RegularExpressions.Regex.Matches(skillsText, @"[^\s,\n、，]+");
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Value.Length > 1 && match.Value.Length < 20)
                    skillTags.Add(match.Value);
            }
        }
        else
        {
            var tagMatches = System.Text.RegularExpressions.Regex.Matches(llmText, @"[#【\[]([^】\]\n]{2,15})[】\]]");
            foreach (System.Text.RegularExpressions.Match match in tagMatches)
            {
                skillTags.Add(match.Groups[1].Value);
            }
        }

        if (skillTags.Count > 15)
            skillTags = skillTags.Take(15).ToList();

        return (title, content, skillTags.Count > 0 ? JsonSerializer.Serialize(skillTags) : null);
    }

    private string GetChannelPrompt(string channelType, JobPost jobPost)
    {
        var baseInfo = $@"原始职位信息：
- 职位名称：{jobPost.Title}
- 工作城市：{jobPost.Department ?? "未指定"}
- 薪资范围：{jobPost.SalaryMin ?? 0}K-{jobPost.SalaryMax ?? 0}K
- 职位描述：{jobPost.Description ?? "未提供"}
- 技能要求：{jobPost.Requirements ?? "未提供"}";

        return channelType switch
        {
            "boss" => $@"你是一个专业的招聘文案专家。请将以下职位描述改编为Boss直聘招聘平台的格式。

{baseInfo}

要求：
1. 标题格式：`[城市]职位名称 | 薪资范围 | 学历要求`
2. 第一段：职位亮点（3条，用·分隔）
3. 第二段：职位描述（200字，简练）
4. 第三段：技能要求（5~8个标签）
5. 第四段：公司介绍（50字）
6. 总字数：500~800字

请直接输出改编后的内容。",

            "liepin" => $@"你是一个专业的招聘文案专家。请将以下职位描述改编为猎聘招聘平台的格式。

{baseInfo}

要求：
1. 标题格式：`职位名称（城市·薪资）`
2. 职位概要：100字，概括核心职责
3. 岗位职责：3~5条，每条30字左右
4. 任职要求：3~5条，每条30字左右
5. 技能标签：5~8个，从原技能要求中提取
6. 总字数：600~1000字

请直接输出改编后的内容，格式如下：
## 职位概要
[100字概要]

## 岗位职责
1. [第一条]
2. [第二条]
3. [第三条]

## 任职要求
1. [第一条]
2. [第二条]
3. [第三条]

## 技能标签
[标签1] [标签2] [标签3] ...",

            "lagou" => $@"你是一个专业的招聘文案专家。请将以下职位描述改编为拉勾招聘平台的格式。

{baseInfo}

要求：
1. 标题格式：`职位名称_公司名_城市`
2. 职位诱惑（3条）
3. 职位描述（3~5条职责）
4. 任职要求（3~5条要求）
5. 技能要求（标签形式）
6. 总字数：400~700字

请直接输出改编后的内容。",

            "linkedin" => $@"You are a professional recruitment copywriter. Adapt the following job posting for LinkedIn format.

Job Information:
- Title: {jobPost.Title}
- Location: {jobPost.Department ?? "Not specified"}
- Salary: {jobPost.SalaryMin ?? 0}K-{jobPost.SalaryMax ?? 0}K
- Description: {jobPost.Description ?? "Not provided"}
- Requirements: {jobPost.Requirements ?? "Not provided"}

Requirements:
1. Title format: `Title at Company Name`
2. About the role (80 words)
3. Responsibilities (3-5 bullet points)
4. Requirements (3-5 bullet points)
5. Skills (tag list)
6. Total: 300-600 words (in English)

Please output the adapted content directly.",

            "xiaohongshu" => $@"你是一个专业的小红书招聘文案专家。请将以下职位描述改编为小红书招聘笔记的格式。

{baseInfo}

要求：
1. 标题格式：`[招聘]职位名称 | 城市 | 薪资`
2. 开场白（emoji + 20字吸引句）
3. 职位亮点（3条，每条带emoji）
4. 职位描述（简洁）
5. 技能要求（标签）
6. 投递方式
7. 总字数：200~400字（口语化，轻松风格）

请直接输出改编后的内容。",

            "custom" => $@"你是一个专业的招聘文案专家。请将以下职位描述改编为自定义格式。

{baseInfo}

要求：
1. 生成一个吸引人的职位标题
2. 职位亮点（3~5条）
3. 详细的职位描述
4. 任职要求（3~5条）
5. 技能标签（5~10个）
6. 保持内容专业且有吸引力

请直接输出改编后的内容。",

            _ => throw new ArgumentException($"Unknown channel type: {channelType}")
        };
    }

    public async Task<List<JobChannelContentDto>> GetAllByJobPostIdAsync(int jobPostId)
    {
        var contents = await _dbContext.JobChannelContents
            .Where(c => c.JobPostId == jobPostId)
            .ToListAsync();

        return contents.Select(MapToDto).ToList();
    }

    public async Task<JobChannelContentDto?> GetByChannelTypeAsync(int jobPostId, string channelType)
    {
        var content = await _dbContext.JobChannelContents
            .FirstOrDefaultAsync(c => c.JobPostId == jobPostId && c.ChannelType == channelType);

        return content == null ? null : MapToDto(content);
    }

    public async Task<JobChannelContentDto?> UpdateAsync(int id, UpdateContentRequest request)
    {
        var content = await _dbContext.JobChannelContents.FindAsync(id);
        if (content == null)
            return null;

        if (request.AdaptedTitle != null)
            content.AdaptedTitle = request.AdaptedTitle;

        if (request.AdaptedContent != null)
            content.AdaptedContent = request.AdaptedContent;

        if (request.WordCount.HasValue)
            content.WordCount = request.WordCount.Value;

        if (request.SkillTags != null)
            content.SkillTags = JsonSerializer.Serialize(request.SkillTags);

        if (request.SalaryMin.HasValue)
            content.SalaryMin = request.SalaryMin.Value;

        if (request.SalaryMax.HasValue)
            content.SalaryMax = request.SalaryMax.Value;

        if (request.Status != null)
            content.Status = request.Status;

        content.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return MapToDto(content);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var content = await _dbContext.JobChannelContents.FindAsync(id);
        if (content == null)
            return false;

        _dbContext.JobChannelContents.Remove(content);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    private static JobChannelContentDto MapToDto(JobChannelContent content)
    {
        return new JobChannelContentDto
        {
            Id = content.Id,
            JobPostId = content.JobPostId,
            ChannelType = content.ChannelType,
            ChannelName = content.ChannelName,
            AdaptedTitle = content.AdaptedTitle,
            AdaptedContent = content.AdaptedContent,
            WordCount = content.WordCount,
            SkillTags = string.IsNullOrEmpty(content.SkillTags)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(content.SkillTags) ?? new List<string>(),
            SalaryMin = content.SalaryMin,
            SalaryMax = content.SalaryMax,
            Status = content.Status,
            ErrorMessage = content.ErrorMessage,
            CreatedAt = content.CreatedAt,
            UpdatedAt = content.UpdatedAt
        };
    }
}