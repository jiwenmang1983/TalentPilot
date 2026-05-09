using Microsoft.EntityFrameworkCore;
using TalentPilot.Api.Data;
using TalentPilot.Api.Models.DTOs;
using TalentPilot.Api.Models.Entities;
using TalentPilot.Api.Services;

namespace TalentPilot.Api.Services;

public interface IAIInterviewSessionService
{
    Task<(List<AIInterviewSession> Items, int Total)> GetAllAsync(string? status, long? candidateId, int? jobPostId, int? invitationId, DateTime? dateFrom, DateTime? dateTo, int page, int pageSize);
    Task<AIInterviewSession?> GetByIdAsync(int id);
    Task<AIInterviewSession?> GetByTokenAsync(string token);
    Task<AIInterviewSessionResponse?> CreateAsync(int invitationId);
    Task<AIInterviewSession?> StartAsync(int id);
    Task<AIInterviewSession?> CompleteAsync(int id);
    Task<AIInterviewSession?> CancelAsync(int id);
    Task<SubmitAnswerResponse> SubmitAnswerAsync(int id, SubmitAnswerRequest request);
    Task<QuestionResponse?> GetNextQuestionAsync(int id);
}

public class AIInterviewSessionService : IAIInterviewSessionService
{
    private readonly TalentPilotDbContext _dbContext;
    private readonly IMiniMaxService _miniMaxService;
    private readonly ILogger<AIInterviewSessionService> _logger;
    private static readonly QuestionResponse[] MockQuestions = new[]
    {
        new QuestionResponse { QuestionId = "Q1", QuestionText = "请简单介绍一下你自己", QuestionType = "open", TimeLimit = 120, Tips = "控制在2分钟内" },
        new QuestionResponse { QuestionId = "Q2", QuestionText = "你为什么想应聘这个职位？", QuestionType = "open", TimeLimit = 90, Tips = "结合职位要求回答" },
        new QuestionResponse { QuestionId = "Q3", QuestionText = "你最大的优势是什么？", QuestionType = "strength", TimeLimit = 60, Tips = "结合具体案例" },
        new QuestionResponse { QuestionId = "Q4", QuestionText = "你如何处理工作中的压力？", QuestionType = "behavioral", TimeLimit = 60, Tips = "STAR法则回答" },
        new QuestionResponse { QuestionId = "Q5", QuestionText = "请描述一个你解决过的难题", QuestionType = "behavioral", TimeLimit = 120, Tips = "突出你的能力和结果" },
    };

    public AIInterviewSessionService(TalentPilotDbContext dbContext, IMiniMaxService miniMaxService, ILogger<AIInterviewSessionService> logger)
    {
        _dbContext = dbContext;
        _miniMaxService = miniMaxService;
        _logger = logger;
    }

    private List<QuestionResponse>? ParseGeneratedQuestions(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("questions", out var questions))
            {
                var result = new List<QuestionResponse>();
                foreach (var q in questions.EnumerateArray())
                {
                    result.Add(new QuestionResponse
                    {
                        QuestionId = q.GetProperty("questionId").GetString() ?? "",
                        QuestionText = q.GetProperty("questionText").GetString() ?? "",
                        QuestionType = q.GetProperty("questionType").GetString() ?? "",
                        TimeLimit = q.GetProperty("timeLimit").GetInt32(),
                        Tips = q.TryGetProperty("tips", out var tips) ? tips.GetString() : null
                    });
                }
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse generated questions JSON");
        }
        return null;
    }

    private async Task<string?> GenerateQuestionsWithLLMAsync(JobPost? jobPost, Candidate? candidate)
    {
        var requirements = jobPost?.Requirements ?? "未提供";
        var candidateInfo = candidate != null
            ? $"姓名: {candidate.Name}, 当前职位: {candidate.CurrentPosition}, 当前公司: {candidate.CurrentCompany}, 技能: {candidate.Skills}, 工作经验: {candidate.WorkExperience}年"
            : "候选人信息未提供";

        var jsonTemplate = "{{\"questions\": [{\"questionId\": \"Q1\", \"questionText\": \"...\", \"questionType\": \"technical\", \"timeLimit\": 120, \"tips\": \"...\"}]}}";

        var prompt = $@"你是一个专业的AI面试官。请根据以下职位要求和候选人信息，生成5-8道个性化面试问题。

职位名称: {jobPost?.Title}
职位要求: {requirements}
候选人信息: {candidateInfo}

请生成包含以下类型的面试题：
1. 技术问题 (questionType: technical) - 考察专业技能
2. 行为问题 (questionType: behavioral) - 考察软技能和经验
3. 情景问题 (questionType: situational) - 考察应变能力

每道题包含：questionId, questionText, questionType, timeLimit(秒), tips(答题提示)。

请以以下JSON格式返回：
{jsonTemplate}

只返回JSON，不要有其他内容。";

        try
        {
            var response = await _miniMaxService.ChatAsync(prompt, 2048);
            if (response?.Content != null && response.Content.Count > 0)
            {
                var text = response.Content[0].Text;
                // Try to extract JSON from the response (in case there's any wrapper text)
                var jsonStart = text.IndexOf('{');
                var jsonEnd = text.LastIndexOf('}');
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    return text.Substring(jsonStart, jsonEnd - jsonStart + 1);
                }
                return text;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate questions with LLM");
        }
        return null;
    }

    public async Task<(List<AIInterviewSession> Items, int Total)> GetAllAsync(
        string? status, long? candidateId, int? jobPostId, int? invitationId,
        DateTime? dateFrom, DateTime? dateTo, int page, int pageSize)
    {
        var query = _dbContext.AIInterviewSessions
            .Include(s => s.Candidate)
            .Include(s => s.JobPost)
            .Include(s => s.InterviewInvitation)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(s => s.Status == status);

        if (candidateId.HasValue)
            query = query.Where(s => s.CandidateId == candidateId.Value);

        if (jobPostId.HasValue)
            query = query.Where(s => s.JobPostId == jobPostId.Value);

        if (invitationId.HasValue)
            query = query.Where(s => s.InterviewInvitationId == invitationId.Value);

        if (dateFrom.HasValue)
            query = query.Where(s => s.CreatedAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(s => s.CreatedAt <= dateTo.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<AIInterviewSession?> GetByIdAsync(int id)
    {
        return await _dbContext.AIInterviewSessions
            .Include(s => s.Candidate)
            .Include(s => s.JobPost)
            .Include(s => s.InterviewInvitation)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<AIInterviewSession?> GetByTokenAsync(string token)
    {
        return await _dbContext.AIInterviewSessions
            .Include(s => s.Candidate)
            .Include(s => s.JobPost)
            .FirstOrDefaultAsync(s => s.SessionToken == token);
    }

    public async Task<AIInterviewSessionResponse?> CreateAsync(int invitationId)
    {
        var invitation = await _dbContext.InterviewInvitations
            .Include(i => i.Candidate)
            .Include(i => i.JobPost)
            .FirstOrDefaultAsync(i => i.Id == invitationId);

        if (invitation == null) return null;

        var session = new AIInterviewSession
        {
            InterviewInvitationId = invitationId,
            CandidateId = invitation.CandidateId,
            JobPostId = invitation.JobPostId,
            SessionToken = Guid.NewGuid().ToString(),
            Status = AIInterviewSessionStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Generate personalized questions with MiniMax LLM
        var generatedQuestionsJson = await GenerateQuestionsWithLLMAsync(invitation.JobPost, invitation.Candidate);
        if (!string.IsNullOrEmpty(generatedQuestionsJson))
        {
            session.GeneratedQuestions = generatedQuestionsJson;
            _logger.LogInformation("Generated personalized interview questions for session {SessionId}", session.Id);
        }
        else
        {
            _logger.LogWarning("Failed to generate personalized questions, will use fallback mock questions");
        }

        _dbContext.AIInterviewSessions.Add(session);
        await _dbContext.SaveChangesAsync();

        return new AIInterviewSessionResponse
        {
            Id = session.Id,
            InterviewInvitationId = session.InterviewInvitationId,
            CandidateId = session.CandidateId,
            CandidateName = invitation.Candidate?.Name,
            JobPostId = session.JobPostId,
            JobPostTitle = invitation.JobPost?.Title,
            SessionToken = session.SessionToken,
            Status = session.Status,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt
        };
    }

    public async Task<AIInterviewSession?> StartAsync(int id)
    {
        var session = await _dbContext.AIInterviewSessions.FindAsync(id);
        if (session == null) return null;

        if (session.Status != AIInterviewSessionStatus.Pending)
            return null;

        session.Status = AIInterviewSessionStatus.InProgress;
        session.StartTime = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return session;
    }

    public async Task<AIInterviewSession?> CompleteAsync(int id)
    {
        var session = await _dbContext.AIInterviewSessions.FindAsync(id);
        if (session == null) return null;

        if (session.Status != AIInterviewSessionStatus.InProgress)
            return null;

        session.Status = AIInterviewSessionStatus.Completed;
        session.EndTime = DateTime.UtcNow;
        if (session.StartTime.HasValue)
            session.DurationSeconds = (int)(session.EndTime.Value - session.StartTime.Value).TotalSeconds;
        session.OverallScore = "85";
        session.AiComments = "候选人在面试中表现良好，具备良好的沟通能力和问题解决能力。";
        session.RecordingUrl = $"https://mock-recording.talentpilot.com/{session.Id}";
        session.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return session;
    }

    public async Task<AIInterviewSession?> CancelAsync(int id)
    {
        var session = await _dbContext.AIInterviewSessions.FindAsync(id);
        if (session == null) return null;

        if (session.Status == AIInterviewSessionStatus.Completed)
            return null;

        session.Status = AIInterviewSessionStatus.Cancelled;
        session.EndTime = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return session;
    }

    public async Task<SubmitAnswerResponse> SubmitAnswerAsync(int id, SubmitAnswerRequest request)
    {
        var session = await _dbContext.AIInterviewSessions.FindAsync(id);
        if (session == null)
            return new SubmitAnswerResponse { Progress = 0, TotalQuestions = MockQuestions.Length };

        var transcriptList = string.IsNullOrEmpty(session.Transcript)
            ? new List<object>()
            : System.Text.Json.JsonSerializer.Deserialize<List<object>>(session.Transcript) ?? new List<object>();

        transcriptList.Add(new
        {
            questionId = request.QuestionId,
            answer = request.Answer,
            audioUrl = request.AudioUrl,
            timestamp = DateTime.UtcNow
        });

        session.Transcript = System.Text.Json.JsonSerializer.Serialize(transcriptList);
        session.UpdatedAt = DateTime.UtcNow;

        // Get total questions from GeneratedQuestions or fallback to MockQuestions
        var questions = ParseGeneratedQuestions(session.GeneratedQuestions);
        var totalQuestions = questions?.Count ?? MockQuestions.Length;

        var answeredCount = transcriptList.Count;
        var progress = answeredCount;
        QuestionResponse? nextQuestion = null;

        if (questions != null && questions.Count > 0)
        {
            if (answeredCount < questions.Count)
            {
                nextQuestion = questions[answeredCount];
            }
        }
        else if (answeredCount < MockQuestions.Length)
        {
            nextQuestion = MockQuestions[answeredCount];
        }

        if (answeredCount >= totalQuestions && session.Status == AIInterviewSessionStatus.InProgress)
        {
            session.Status = AIInterviewSessionStatus.Completed;
            session.EndTime = DateTime.UtcNow;
            if (session.StartTime.HasValue)
                session.DurationSeconds = (int)(session.EndTime.Value - session.StartTime.Value).TotalSeconds;
            session.OverallScore = "85";
            session.AiComments = "面试完成，候选人回答了所有问题。";
            session.RecordingUrl = $"https://mock-recording.talentpilot.com/{session.Id}";
        }

        await _dbContext.SaveChangesAsync();

        return new SubmitAnswerResponse
        {
            NextQuestion = nextQuestion,
            Progress = progress,
            TotalQuestions = totalQuestions
        };
    }

    public async Task<QuestionResponse?> GetNextQuestionAsync(int id)
    {
        var session = await _dbContext.AIInterviewSessions.FindAsync(id);
        if (session == null) return null;

        var answeredCount = string.IsNullOrEmpty(session.Transcript)
            ? 0
            : (System.Text.Json.JsonSerializer.Deserialize<List<object>>(session.Transcript)?.Count ?? 0);

        // Try to get questions from GeneratedQuestions first
        var questions = ParseGeneratedQuestions(session.GeneratedQuestions);
        if (questions != null && questions.Count > 0)
        {
            if (answeredCount >= questions.Count)
                return null;
            return questions[answeredCount];
        }

        // Fallback to mock questions
        if (answeredCount >= MockQuestions.Length)
            return null;

        return MockQuestions[answeredCount];
    }
}
