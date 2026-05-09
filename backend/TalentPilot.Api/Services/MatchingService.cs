using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TalentPilot.Api.Data;
using TalentPilot.Api.Models.Entities;

namespace TalentPilot.Api.Services;

public interface IMatchingService
{
    Task<MatchResult?> CalculateMatchAsync(int resumeId, int jobPostId);
    Task<List<MatchResult>> BatchMatchAsync(int jobPostId);
    Task<(List<MatchResult> Items, int Total)> GetMatchResultsAsync(int? jobPostId, int page, int pageSize);
    Task<MatchResult?> UpdateMatchStatusAsync(int matchId, string status);
}

public class MatchingService : IMatchingService
{
    private readonly TalentPilotDbContext _dbContext;
    private readonly IMiniMaxService _miniMaxService;
    private readonly ILogger<MatchingService> _logger;

    public MatchingService(TalentPilotDbContext dbContext, IMiniMaxService miniMaxService, ILogger<MatchingService> logger)
    {
        _dbContext = dbContext;
        _miniMaxService = miniMaxService;
        _logger = logger;
    }

    public async Task<MatchResult?> CalculateMatchAsync(int resumeId, int jobPostId)
    {
        var resume = await _dbContext.Resumes.FindAsync(resumeId);
        var jobPost = await _dbContext.JobPosts.FindAsync(jobPostId);

        if (resume == null || jobPost == null)
            return null;

        var candidate = resume.CandidateId.HasValue
            ? await _dbContext.Candidates.FindAsync(resume.CandidateId)
            : null;

        // Check if already matched
        var existing = await _dbContext.MatchResults
            .FirstOrDefaultAsync(m => m.ResumeId == resumeId && m.JobPostId == jobPostId);

        // Build prompt for MiniMax LLM
        var prompt = BuildMatchPrompt(candidate, resume, jobPost);

        // Call MiniMax LLM
        var llmResponse = await _miniMaxService.ChatAsync(prompt, 1024);

        decimal score;
        string skillMatch = "";
        string experienceMatch = "";
        string salaryMatch = "";
        string reason = "";
        var matchedSkills = new List<string>();
        var missingSkills = new List<string>();

        if (llmResponse?.Content != null && llmResponse.Content.Count > 0)
        {
            var responseText = llmResponse.GetFirstText() ?? "";
            try
            {
                // Try to parse JSON from response
                var jsonMatch = System.Text.RegularExpressions.Regex.Match(responseText, @"\{[\s\S]*\}");
                if (jsonMatch.Success)
                {
                    var jsonDoc = JsonDocument.Parse(jsonMatch.Value);
                    var root = jsonDoc.RootElement;

                    if (root.TryGetProperty("score", out var scoreElement))
                        score = scoreElement.GetDecimal();
                    else
                        score = 50;

                    if (root.TryGetProperty("skillMatch", out var skillMatchElement))
                        skillMatch = skillMatchElement.GetString() ?? "";
                    if (root.TryGetProperty("experienceMatch", out var expMatchElement))
                        experienceMatch = expMatchElement.GetString() ?? "";
                    if (root.TryGetProperty("salaryMatch", out var salaryMatchElement))
                        salaryMatch = salaryMatchElement.GetString() ?? "";
                    if (root.TryGetProperty("reason", out var reasonElement))
                        reason = reasonElement.GetString() ?? "";

                    // Extract skills from skillMatch if present
                    var skillKeywords = new[] { "Java", "Python", "Go", "JavaScript", "TypeScript", "React", "Vue", "Angular",
                        "Spring", "Django", "Node.js", "Docker", "Kubernetes", "AWS", "Azure", "MySQL", "Redis",
                        "Machine Learning", "TensorFlow", "Agile", "Scrum" };
                    foreach (var kw in skillKeywords)
                    {
                        if (skillMatch.Contains(kw, StringComparison.OrdinalIgnoreCase) &&
                            !matchedSkills.Contains(kw, StringComparer.OrdinalIgnoreCase))
                            matchedSkills.Add(kw);
                    }
                }
                else
                {
                    score = 50;
                    reason = responseText.Length > 500 ? responseText[..500] : responseText;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse LLM response as JSON, using default score");
                score = 50;
                reason = responseText.Length > 500 ? responseText[..500] : responseText;
            }
        }
        else
        {
            score = 50;
            reason = "LLM调用未返回有效响应，使用默认分数";
        }

        var summary = $"{candidate?.Name ?? resume.CandidateName ?? "候选人"}与职位【{jobPost.Title}】匹配度{Math.Round(score, 0)}%\n" +
                     $"技能匹配: {skillMatch}\n" +
                     $"经验匹配: {experienceMatch}\n" +
                     $"薪资匹配: {salaryMatch}\n" +
                     $"综合评价: {reason}";

        if (existing != null)
        {
            existing.Score = Math.Round(score, 2);
            existing.MatchedSkills = JsonSerializer.Serialize(matchedSkills);
            existing.MissingSkills = JsonSerializer.Serialize(missingSkills);
            existing.Summary = summary;
            existing.CreatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            return existing;
        }

        var result = new MatchResult
        {
            ResumeId = resumeId,
            JobPostId = jobPostId,
            Score = Math.Round(score, 2),
            MatchedSkills = JsonSerializer.Serialize(matchedSkills),
            MissingSkills = JsonSerializer.Serialize(missingSkills),
            Summary = summary,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.MatchResults.Add(result);
        await _dbContext.SaveChangesAsync();
        return result;
    }

    private static string BuildMatchPrompt(Candidate? candidate, Resume resume, JobPost jobPost)
    {
        var candidateName = candidate?.Name ?? resume.CandidateName ?? "未知";
        var skills = candidate?.Skills ?? "未提供";
        var education = candidate?.Education ?? "未提供";
        var workExperience = candidate?.WorkExperience?.ToString() ?? "未提供";
        var currentPosition = candidate?.CurrentPosition ?? "未提供";
        var currentCompany = candidate?.CurrentCompany ?? "未提供";
        var expectedSalary = candidate?.ExpectedSalary?.ToString() ?? "未提供";
        var jobTitle = jobPost.Title ?? "未知职位";
        var requirements = jobPost.Requirements ?? "未提供";
        var salaryMin = jobPost.SalaryMin?.ToString() ?? "未指定";
        var salaryMax = jobPost.SalaryMax?.ToString() ?? "未指定";
        var experience = jobPost.Experience ?? "未指定";
        var jobEducation = jobPost.Education ?? "未指定";

        return $@"你是一个专业的招聘匹配系统。请分析以下候选人与职位的匹配程度。

【职位信息】
职位名称: {jobTitle}
职位要求: {requirements}
薪资范围: {salaryMin} - {salaryMax}
经验要求: {experience}
学历要求: {jobEducation}

【候选人信息】
姓名: {candidateName}
当前职位: {currentPosition}
当前公司: {currentCompany}
工作年限: {workExperience}
学历: {education}
技能: {skills}
期望薪资: {expectedSalary}

请返回JSON格式的匹配结果:
{{
    ""score"": 0-100的匹配分数,
    ""skillMatch"": ""技能匹配分析"",
    ""experienceMatch"": ""经验匹配分析"",
    ""salaryMatch"": ""薪资匹配分析"",
    ""reason"": ""综合评价""
}}

只返回JSON，不要有其他内容。";
    }

    public async Task<List<MatchResult>> BatchMatchAsync(int jobPostId)
    {
        var jobPost = await _dbContext.JobPosts.FindAsync(jobPostId);
        if (jobPost == null) return new List<MatchResult>();

        var resumes = await _dbContext.Resumes
            .Where(r => r.ParsedStatus == "Success" && r.CandidateId.HasValue)
            .ToListAsync();

        var results = new List<MatchResult>();
        foreach (var resume in resumes)
        {
            var result = await CalculateMatchAsync(resume.Id, jobPostId);
            if (result != null)
            {
                results.Add(result);
            }
        }

        return results;
    }

    public async Task<(List<MatchResult> Items, int Total)> GetMatchResultsAsync(int? jobPostId, int page, int pageSize)
    {
        var query = _dbContext.MatchResults.AsQueryable();

        if (jobPostId.HasValue)
        {
            query = query.Where(m => m.JobPostId == jobPostId.Value);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(m => m.Score)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<MatchResult?> UpdateMatchStatusAsync(int matchId, string status)
    {
        var match = await _dbContext.MatchResults.FindAsync(matchId);
        if (match == null) return null;

        match.Status = status;
        await _dbContext.SaveChangesAsync();
        return match;
    }

    private static List<string> ParseSkills(string? skillsJson)
    {
        if (string.IsNullOrEmpty(skillsJson)) return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(skillsJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static List<string> ExtractKeywords(string text)
    {
        if (string.IsNullOrEmpty(text)) return new List<string>();

        var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Java", "Spring", "MySQL", "Redis", "Docker", "Kubernetes",
            "Vue", "React", "Angular", "TypeScript", "Node.js",
            "Python", "Django", "Flask", "Machine Learning", "TensorFlow",
            "Go", "gRPC", "Microservices", "AWS", "Azure",
            "Product Management", "Agile", "Scrum", "Data Analysis"
        };

        return keywords.Where(k => text.Contains(k, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private static string GenerateSummary(string candidateName, List<string> matched, List<string> missing, decimal score)
    {
        return $"{candidateName}与职位匹配度{Math.Round(score, 0)}%，" +
               $"匹配技能：{string.Join(", ", matched.Take(3))}，" +
               $"缺失技能：{string.Join(", ", missing.Take(3))}";
    }
}
