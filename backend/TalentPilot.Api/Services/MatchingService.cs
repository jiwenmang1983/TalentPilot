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

    // Default weights for 8 dimensions
    private static readonly Dictionary<string, decimal> DefaultWeights = new()
    {
        { "skillWeight", 30 },
        { "experienceWeight", 20 },
        { "educationWeight", 15 },
        { "industryWeight", 10 },
        { "levelWeight", 10 },
        { "salaryWeight", 5 },
        { "locationWeight", 5 },
        { "turnoverWeight", 5 }
    };

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

        // Get job post weights or use defaults
        var weights = ParseWeights(jobPost.MatchWeights) ?? DefaultWeights;

        // Check if already matched
        var existing = await _dbContext.MatchResults
            .FirstOrDefaultAsync(m => m.ResumeId == resumeId && m.JobPostId == jobPostId);

        // Build prompt for MiniMax LLM
        var prompt = BuildMatchPrompt(candidate, resume, jobPost);

        // Call MiniMax LLM
        var llmResponse = await _miniMaxService.ChatAsync(prompt, 1024);

        // Default values
        decimal skillScore = 50, experienceScore = 50, educationScore = 50;
        decimal industryScore = 50, levelScore = 50, salaryScore = 50;
        decimal locationScore = 50, turnoverScore = 50;
        string skillMatch = "", experienceMatch = "", educationMatch = "";
        string industryMatch = "", levelMatch = "", salaryMatch = "";
        string locationMatch = "", turnoverMatch = "";
        decimal totalScore = 50;

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

                    // Parse 8 dimension scores
                    if (root.TryGetProperty("skillScore", out var s))
                        skillScore = s.TryGetDecimal(out var sv) ? sv : ParseDecimalFromOther(s);
                    if (root.TryGetProperty("experienceScore", out var e))
                        experienceScore = e.TryGetDecimal(out var ev) ? ev : ParseDecimalFromOther(e);
                    if (root.TryGetProperty("educationScore", out var ed))
                        educationScore = ed.TryGetDecimal(out var edv) ? edv : ParseDecimalFromOther(ed);
                    if (root.TryGetProperty("industryScore", out var i))
                        industryScore = i.TryGetDecimal(out var iv) ? iv : ParseDecimalFromOther(i);
                    if (root.TryGetProperty("levelScore", out var l))
                        levelScore = l.TryGetDecimal(out var lv) ? lv : ParseDecimalFromOther(l);
                    if (root.TryGetProperty("salaryScore", out var sa))
                        salaryScore = sa.TryGetDecimal(out var sav) ? sav : ParseDecimalFromOther(sa);
                    if (root.TryGetProperty("locationScore", out var lo))
                        locationScore = lo.TryGetDecimal(out var lov) ? lov : ParseDecimalFromOther(lo);
                    if (root.TryGetProperty("turnoverScore", out var t))
                        turnoverScore = t.TryGetDecimal(out var tv) ? tv : ParseDecimalFromOther(t);

                    // Parse match analysis texts
                    if (root.TryGetProperty("skillMatch", out var sm)) skillMatch = sm.GetString() ?? "";
                    if (root.TryGetProperty("experienceMatch", out var em)) experienceMatch = em.GetString() ?? "";
                    if (root.TryGetProperty("educationMatch", out var edm)) educationMatch = edm.GetString() ?? "";
                    if (root.TryGetProperty("industryMatch", out var im)) industryMatch = im.GetString() ?? "";
                    if (root.TryGetProperty("levelMatch", out var lm)) levelMatch = lm.GetString() ?? "";
                    if (root.TryGetProperty("salaryMatch", out var sam)) salaryMatch = sam.GetString() ?? "";
                    if (root.TryGetProperty("locationMatch", out var lom)) locationMatch = lom.GetString() ?? "";
                    if (root.TryGetProperty("turnoverMatch", out var tm)) turnoverMatch = tm.GetString() ?? "";

                    // Calculate total score using weighted formula
                    totalScore = CalculateTotalScore(skillScore, experienceScore, educationScore,
                        industryScore, levelScore, salaryScore, locationScore, turnoverScore, weights);
                }
                else
                {
                    // Fallback: try to parse old format or use default
                    totalScore = 50;
                    skillMatch = responseText.Length > 500 ? responseText[..500] : responseText;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse LLM response as JSON, using default score");
                totalScore = 50;
            }
        }

        // Build dimension scores JSON
        var dimensionScores = new Dictionary<string, decimal?>
        {
            { "skillScore", Math.Round(skillScore, 2) },
            { "experienceScore", Math.Round(experienceScore, 2) },
            { "educationScore", Math.Round(educationScore, 2) },
            { "industryScore", Math.Round(industryScore, 2) },
            { "levelScore", Math.Round(levelScore, 2) },
            { "salaryScore", Math.Round(salaryScore, 2) },
            { "locationScore", Math.Round(locationScore, 2) },
            { "turnoverScore", Math.Round(turnoverScore, 2) }
        };

        // Build summary text
        var summary = $"{candidate?.Name ?? resume.CandidateName ?? "候选人"}与职位【{jobPost.Title}】匹配度{Math.Round(totalScore, 0)}%\n" +
                     $"技能匹配: {skillMatch}\n" +
                     $"经验匹配: {experienceMatch}\n" +
                     $"学历匹配: {educationMatch}\n" +
                     $"行业匹配: {industryMatch}\n" +
                     $"级别匹配: {levelMatch}\n" +
                     $"薪资匹配: {salaryMatch}\n" +
                     $"地理匹配: {locationMatch}\n" +
                     $"稳定性: {turnoverMatch}";

        // Serialize matched skills
        var matchedSkills = new List<string>();
        var skillKeywords = new[] { "Java", "Python", "Go", "JavaScript", "TypeScript", "React", "Vue", "Angular",
            "Spring", "Django", "Node.js", "Docker", "Kubernetes", "AWS", "Azure", "MySQL", "Redis",
            "Machine Learning", "TensorFlow", "Agile", "Scrum" };
        foreach (var kw in skillKeywords)
        {
            if (skillMatch.Contains(kw, StringComparison.OrdinalIgnoreCase) &&
                !matchedSkills.Contains(kw, StringComparer.OrdinalIgnoreCase))
                matchedSkills.Add(kw);
        }

        if (existing != null)
        {
            existing.Score = Math.Round(totalScore, 2);
            existing.MatchedSkills = JsonSerializer.Serialize(matchedSkills);
            existing.DimensionScores = JsonSerializer.Serialize(dimensionScores);
            existing.DimensionWeights = JsonSerializer.Serialize(weights);
            existing.MatchThreshold = jobPost.MatchThreshold ?? 80;
            existing.Summary = summary;
            existing.CreatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            return existing;
        }

        var result = new MatchResult
        {
            ResumeId = resumeId,
            JobPostId = jobPostId,
            Score = Math.Round(totalScore, 2),
            MatchedSkills = JsonSerializer.Serialize(matchedSkills),
            DimensionScores = JsonSerializer.Serialize(dimensionScores),
            DimensionWeights = JsonSerializer.Serialize(weights),
            MatchThreshold = jobPost.MatchThreshold ?? 80,
            Summary = summary,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.MatchResults.Add(result);
        await _dbContext.SaveChangesAsync();
        return result;
    }

    private static decimal ParseDecimalFromOther(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Number:
                return element.GetDouble() > 100 ? (decimal)(element.GetDouble() / 100.0 * 100) : (decimal)element.GetDouble();
            case JsonValueKind.String:
                var str = element.GetString() ?? "50";
                str = new string(str.Where(c => char.IsDigit(c) || c == '.').ToArray());
                return decimal.TryParse(str, out var d) ? d : 50;
            default:
                return 50;
        }
    }

    private static Dictionary<string, decimal> ParseWeights(string? weightsJson)
    {
        if (string.IsNullOrEmpty(weightsJson)) return null;
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, decimal>>(weightsJson);
            return dict;
        }
        catch
        {
            return null;
        }
    }

    private static decimal CalculateTotalScore(decimal skill, decimal experience, decimal education,
        decimal industry, decimal level, decimal salary, decimal location, decimal turnover,
        Dictionary<string, decimal> weights)
    {
        decimal total = skill * (weights.GetValueOrDefault("skillWeight", 30) / 100m)
                       + experience * (weights.GetValueOrDefault("experienceWeight", 20) / 100m)
                       + education * (weights.GetValueOrDefault("educationWeight", 15) / 100m)
                       + industry * (weights.GetValueOrDefault("industryWeight", 10) / 100m)
                       + level * (weights.GetValueOrDefault("levelWeight", 10) / 100m)
                       + salary * (weights.GetValueOrDefault("salaryWeight", 5) / 100m)
                       + location * (weights.GetValueOrDefault("locationWeight", 5) / 100m)
                       + turnover * (weights.GetValueOrDefault("turnoverWeight", 5) / 100m);
        return Math.Round(total, 2);
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

        return $@"你是一个专业的招聘匹配系统。请分析以下候选人与职位的匹配程度，返回8维度评分和详细分析。

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

请返回JSON格式的匹配结果，包含8维度评分：
{{
    ""skillScore"": 0-100的技能匹配分数,
    ""experienceScore"": 0-100的年限匹配分数,
    ""educationScore"": 0-100的学历匹配分数,
    ""industryScore"": 0-100的行业匹配分数,
    ""levelScore"": 0-100的级别匹配分数,
    ""salaryScore"": 0-100的薪资匹配分数(无数据时填null),
    ""locationScore"": 0-100的地理位置匹配分数,
    ""turnoverScore"": 0-100的稳定性/跳槽频率分数(无数据时填null),
    ""skillMatch"": ""技能匹配详细分析"",
    ""experienceMatch"": ""经验匹配详细分析"",
    ""educationMatch"": ""学历匹配详细分析"",
    ""industryMatch"": ""行业匹配详细分析"",
    ""levelMatch"": ""级别匹配详细分析"",
    ""salaryMatch"": ""薪资匹配详细分析(无数据则填'未提供')"",
    ""locationMatch"": ""地理位置匹配详细分析"",
    ""turnoverMatch"": ""稳定性分析(无数据则填'未提供')""
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
}