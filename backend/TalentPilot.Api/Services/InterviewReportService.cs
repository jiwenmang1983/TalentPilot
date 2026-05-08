using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TalentPilot.Api.Data;
using TalentPilot.Api.Models.Entities;

namespace TalentPilot.Api.Services;

public interface IInterviewReportService
{
    Task<InterviewReport?> GetByIdAsync(int id);
    Task<InterviewReport?> GetBySessionIdAsync(long sessionId);
    Task<(List<InterviewReport> Items, int Total)> GetAllAsync(
        long? candidateId, int? jobPostId, string? recommendation,
        decimal? minScore, decimal? maxScore,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize);
    Task<InterviewReport> GenerateReportAsync(long sessionId);
    Task<InterviewReport?> UpdateHrNotesAsync(int id, string hrNotes);
}

public class InterviewReportService : IInterviewReportService
{
    private readonly TalentPilotDbContext _dbContext;

    public InterviewReportService(TalentPilotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<InterviewReport?> GetByIdAsync(int id)
    {
        return await _dbContext.InterviewReports
            .Include(r => r.Candidate)
            .Include(r => r.JobPost)
            .Include(r => r.AIInterviewSession)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<InterviewReport?> GetBySessionIdAsync(long sessionId)
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

    public async Task<InterviewReport> GenerateReportAsync(long sessionId)
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
        var totalTextLength = CountTextLength(transcript);

        // Mock scoring logic
        decimal overallScore = Math.Min(95, 60 + (totalTextLength / 10));

        string scoreText = overallScore switch
        {
            >= 85 => "优秀",
            >= 75 => "良好",
            >= 60 => "一般",
            _ => "待改进"
        };

        string recommendation = overallScore switch
        {
            >= 85 => "StrongHire",
            >= 75 => "Hire",
            >= 60 => "Hold",
            _ => "Reject"
        };

        // Generate dimension scores
        var dimensionScores = new Dictionary<string, decimal>
        {
            { "技术能力", Math.Min(100, overallScore + RandomScore()) },
            { "沟通表达", Math.Min(100, overallScore - 5 + RandomScore()) },
            { "问题解决", Math.Min(100, overallScore + RandomScore()) },
            { "团队协作", Math.Min(100, overallScore - 3 + RandomScore()) },
            { "学习能力", Math.Min(100, overallScore + RandomScore()) }
        };

        // Generate highlights and concerns
        var highlights = DetectHighlights(transcript);
        var concerns = DetectConcerns(transcript);

        // Generate AI comments
        var aiComments = GenerateComments(transcript, overallScore, dimensionScores, highlights, concerns);

        var report = new InterviewReport
        {
            AIInterviewSessionId = sessionId,
            CandidateId = session.CandidateId,
            JobPostId = session.JobPostId,
            OverallScore = overallScore,
            ScoreText = scoreText,
            DimensionScores = JsonSerializer.Serialize(dimensionScores),
            AiComments = aiComments,
            Recommendation = recommendation,
            Highlights = JsonSerializer.Serialize(highlights),
            Concerns = JsonSerializer.Serialize(concerns),
            HrNotes = "",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.InterviewReports.Add(report);
        await _dbContext.SaveChangesAsync();

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

    private static int CountTextLength(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        // Simple Chinese/English text length calculation
        var chineseChars = text.Count(c => c >= 0x4E00 && c <= 0x9FFF);
        var englishWords = text.Split(' ')
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Count();
        return chineseChars + englishWords * 2;
    }

    private static decimal RandomScore()
    {
        var random = new Random();
        return (decimal)(random.NextDouble() * 10 - 5);
    }

    private static List<string> DetectHighlights(string transcript)
    {
        var highlights = new List<string>();
        var lowerTranscript = transcript.ToLower();

        var positiveKeywords = new Dictionary<string, string>
        {
            { "项目", "有大型项目经验" },
            { "团队", "有团队协作经验" },
            { "解决", "具备问题解决能力" },
            { "架构", "了解系统架构设计" },
            { "性能", "关注系统性能优化" },
            { "开源", "参与过开源项目" },
            { "领导", "有团队领导经验" },
            { "培训", "有培训他人经验" },
            { "业绩", "有突出的工作业绩" },
            { "创新", "具有创新思维" }
        };

        foreach (var kvp in positiveKeywords)
        {
            if (lowerTranscript.Contains(kvp.Key))
                highlights.Add(kvp.Value);
        }

        if (highlights.Count < 2)
        {
            highlights.Add("表达清晰有条理");
            highlights.Add("态度积极认真");
        }

        return highlights.Take(5).ToList();
    }

    private static List<string> DetectConcerns(string transcript)
    {
        var concerns = new List<string>();
        var lowerTranscript = transcript.ToLower();

        var negativeKeywords = new Dictionary<string, string>
        {
            { "没有", "部分经验有所欠缺" },
            { "不太会", "某些技能需要提升" },
            { "第一次", "相关经验较少" },
            { "可能", "回答缺乏自信" },
            { "不太清楚", "对某些概念理解不够深入" },
            { "一般", "表现中规中矩" },
            { "加班", "对工作强度有顾虑" },
            { "薪资", "薪资期望可能较高" }
        };

        foreach (var kvp in negativeKeywords)
        {
            if (lowerTranscript.Contains(kvp.Key))
                concerns.Add(kvp.Value);
        }

        return concerns.Take(3).ToList();
    }

    private static string GenerateComments(
        string transcript,
        decimal overallScore,
        Dictionary<string, decimal> dimensionScores,
        List<string> highlights,
        List<string> concerns)
    {
        var topDimension = dimensionScores.OrderByDescending(kv => kv.Value).First();
        var lowDimension = dimensionScores.OrderBy(kv => kv.Value).First();

        var comments = $@"整体评价：
该候选人在面试中表现{(overallScore >= 75 ? "良好" : "一般")}，综合评分{overallScore:F1}分。

维度分析：
- {topDimension.Key}最为突出，得分{topDimension.Value:F1}分
- {lowDimension.Key}相对较弱，得分{lowDimension.Value:F1}分，建议后续加强

{(highlights.Count > 0 ? $"亮点：{string.Join("，", highlights.Take(3))}。" : "")}

{(concerns.Count > 0 ? $"建议关注：{string.Join("，", concerns)}。" : "")}

综合建议：{GetRecommendationAdvice(overallScore)}";

        return comments;
    }

    private static string GetRecommendationAdvice(decimal score)
    {
        return score switch
        {
            >= 85 => "强烈建议录用，候选人综合素质优秀",
            >= 75 => "建议录用，可安排进一步面试",
            >= 60 => "建议观望，与其他候选人综合比较",
            _ => "暂不推荐，建议继续寻找更合适的人选"
        };
    }
}
