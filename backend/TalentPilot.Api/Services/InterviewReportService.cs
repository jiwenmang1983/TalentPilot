using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TalentPilot.Api.Data;
using TalentPilot.Api.Models.Entities;

namespace TalentPilot.Api.Services;

public interface IInterviewReportService
{
    Task<InterviewReport?> GetByIdAsync(int id);
    Task<InterviewReport?> GetBySessionIdAsync(int sessionId);
    Task<(List<InterviewReport> Items, int Total)> GetAllAsync(
        long? candidateId, int? jobPostId, string? recommendation,
        decimal? minScore, decimal? maxScore,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize);
    Task<InterviewReport> GenerateReportAsync(int sessionId);
    Task<InterviewReport?> UpdateHrNotesAsync(int id, string hrNotes);
}

public class InterviewReportService : IInterviewReportService
{
    private readonly TalentPilotDbContext _dbContext;
    private readonly IMiniMaxService _miniMaxService;
    private readonly ILogger<InterviewReportService> _logger;

    public InterviewReportService(
        TalentPilotDbContext dbContext,
        IMiniMaxService miniMaxService,
        ILogger<InterviewReportService> logger)
    {
        _dbContext = dbContext;
        _miniMaxService = miniMaxService;
        _logger = logger;
    }

    public async Task<InterviewReport?> GetByIdAsync(int id)
    {
        return await _dbContext.InterviewReports
            .Include(r => r.Candidate)
            .Include(r => r.JobPost)
            .Include(r => r.AIInterviewSession)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<InterviewReport?> GetBySessionIdAsync(int sessionId)
    {
        return await _dbContext.InterviewReports
            .Include(r => r.Candidate)
            .Include(r => r.JobPost)
            .Include(r => r.AIInterviewSession)
            .FirstOrDefaultAsync(r => r.AIInterviewSessionId == sessionId);
    }

    public async Task<(List<InterviewReport> Items, int Total)> GetAllAsync(
        long? candidateId, int? jobPostId, string? recommendation,
        decimal? minScore, decimal? maxScore,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize)
    {
        var query = _dbContext.InterviewReports
            .Include(r => r.Candidate)
            .Include(r => r.JobPost)
            .AsQueryable();

        if (candidateId.HasValue)
            query = query.Where(r => r.CandidateId == candidateId.Value);

        if (jobPostId.HasValue)
            query = query.Where(r => r.JobPostId == jobPostId.Value);

        if (!string.IsNullOrEmpty(recommendation))
            query = query.Where(r => r.Recommendation == recommendation);

        if (minScore.HasValue)
            query = query.Where(r => r.OverallScore >= minScore.Value);

        if (maxScore.HasValue)
            query = query.Where(r => r.OverallScore <= maxScore.Value);

        if (dateFrom.HasValue)
            query = query.Where(r => r.CreatedAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(r => r.CreatedAt <= dateTo.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<InterviewReport> GenerateReportAsync(int sessionId)
    {
        // Check if report already exists
        var existing = await GetBySessionIdAsync(sessionId);
        if (existing != null)
            return existing;

        // Get session and candidate info
        var session = await _dbContext.AIInterviewSessions
            .Include(s => s.Candidate)
            .Include(s => s.JobPost)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
            throw new InvalidOperationException("AI Interview Session not found");

        // Extract transcript content for analysis
        var transcript = session.Transcript ?? "";

        // Check if transcript is empty or too short
        if (string.IsNullOrWhiteSpace(transcript) || transcript.Length < 50)
        {
            throw new InvalidOperationException("暂无足够面试数据生成报告");
        }

        // Build prompt for MiniMax LLM
        var candidateName = session.Candidate?.Name ?? "未知";
        var jobTitle = session.JobPost?.Title ?? "未知";
        var prompt = BuildAnalysisPrompt(transcript, candidateName, jobTitle);

        _logger.LogInformation("Calling MiniMax LLM for interview report analysis, sessionId: {SessionId}", sessionId);

        // Call MiniMax LLM
        var response = await _miniMaxService.ChatAsync(prompt, maxTokens: 2048);

        if (response == null || response.Content == null || response.Content.Count == 0)
        {
            _logger.LogError("MiniMax API returned null or empty response for sessionId: {SessionId}", sessionId);
            throw new InvalidOperationException("AI分析服务暂时不可用，请稍后重试");
        }

        // Extract JSON from LLM response
        var llmText = response.GetFirstText() ?? "";
        _logger.LogDebug("MiniMax LLM response: {Response}", llmText);

        // Parse JSON response from LLM
        LLMReportResponse? reportData;
        try
        {
            // Try to extract JSON from the response (LLM might wrap it in markdown code blocks)
            var jsonText = ExtractJson(llmText);
            reportData = JsonSerializer.Deserialize<LLMReportResponse>(jsonText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse LLM response as JSON: {Response}", llmText);
            throw new InvalidOperationException("AI分析结果解析失败，请稍后重试");
        }

        if (reportData == null)
        {
            throw new InvalidOperationException("AI分析结果无效，请稍后重试");
        }

        // Create InterviewReport from LLM analysis
        var report = new InterviewReport
        {
            AIInterviewSessionId = sessionId,
            CandidateId = session.CandidateId,
            JobPostId = session.JobPostId,
            OverallScore = reportData.OverallScore,
            ScoreText = reportData.ScoreText,
            DimensionScores = JsonSerializer.Serialize(reportData.DimensionScores),
            AiComments = reportData.AiComments,
            Recommendation = reportData.Recommendation,
            Highlights = JsonSerializer.Serialize(reportData.Highlights),
            Concerns = JsonSerializer.Serialize(reportData.Concerns),
            HrNotes = "",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.InterviewReports.Add(report);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Successfully generated interview report for sessionId: {SessionId}, score: {Score}", sessionId, reportData.OverallScore);

        return report;
    }

    public async Task<InterviewReport?> UpdateHrNotesAsync(int id, string hrNotes)
    {
        var report = await _dbContext.InterviewReports.FindAsync(id);
        if (report == null)
            return null;

        report.HrNotes = hrNotes;
        await _dbContext.SaveChangesAsync();
        return report;
    }

    private static string BuildAnalysisPrompt(string transcript, string candidateName, string jobTitle)
    {
        return $@"你是一位专业的AI面试评估专家。请分析以下面试记录，对候选人进行全面评估。

候选人信息：
- 姓名：{candidateName}
- 应聘岗位：{jobTitle}

面试问答记录：
{transcript}

请根据面试记录，从以下几个维度进行评估：
1. 技术能力（专业知识、项目经验、技术深度）
2. 沟通表达（表达清晰度、逻辑思维）
3. 问题解决（分析问题能力、解决方案思路）
4. 团队协作（合作经验、领导能力）
5. 学习能力（学习主动性、成长潜力）

请以JSON格式返回评估结果，格式如下：
{{
  ""overallScore"": 85,
  ""scoreText"": ""优秀"",
  ""dimensionScores"": {{
    ""技术能力"": 88,
    ""沟通表达"": 82,
    ""问题解决"": 85,
    ""团队协作"": 80,
    ""学习能力"": 83
  }},
  ""highlights"": [""有大型项目经验"", ""问题解决能力强""],
  ""concerns"": [""薪资期望较高""],
  ""aiComments"": ""综合评价..."",
  ""recommendation"": ""Hire""
}}

注意事项：
- overallScore为0-100的整数
- scoreText可选值：优秀、良好、一般、待改进
- recommendation可选值：StrongHire、Hire、Hold、Reject
- highlights和concerns各返回2-5条
- aiComments是对候选人面试表现的综合评价，50-200字
- 请只返回JSON，不要添加任何解释或其他内容";
    }

    private static string ExtractJson(string text)
    {
        // Try to find JSON in markdown code blocks first
        var codeBlockMatch = System.Text.RegularExpressions.Regex.Match(text, @"```(?:json)?\s*(\{[\s\S]*?\})\s*```");
        if (codeBlockMatch.Success)
            return codeBlockMatch.Groups[1].Value;

        // Try to find raw JSON object
        var jsonMatch = System.Text.RegularExpressions.Regex.Match(text, @"\{[\s\S]*\}");
        if (jsonMatch.Success)
            return jsonMatch.Value;

        return text;
    }
}

// Internal class for parsing LLM response
internal class LLMReportResponse
{
    public decimal OverallScore { get; set; }
    public string ScoreText { get; set; } = "";
    public Dictionary<string, decimal> DimensionScores { get; set; } = new();
    public List<string> Highlights { get; set; } = new();
    public List<string> Concerns { get; set; } = new();
    public string AiComments { get; set; } = "";
    public string Recommendation { get; set; } = "";
}
