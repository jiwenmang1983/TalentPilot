namespace TalentPilot.Api.Models.DTOs;

public class AIInterviewSessionResponse
{
    public int Id { get; set; }
    public int InterviewInvitationId { get; set; }
    public long CandidateId { get; set; }
    public string? CandidateName { get; set; }
    public int JobPostId { get; set; }
    public string? JobPostTitle { get; set; }
    public string SessionToken { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int DurationSeconds { get; set; }
    public string? RecordingUrl { get; set; }
    public string? Transcript { get; set; }
    public string? OverallScore { get; set; }
    public string? AiComments { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateAIInterviewSessionRequest
{
    public int InterviewInvitationId { get; set; }
}

public class SubmitAnswerRequest
{
    public string QuestionId { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string? AudioUrl { get; set; }
}

public class QuestionResponse
{
    public string QuestionId { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public int TimeLimit { get; set; }
    public string? Tips { get; set; }
}

public class SubmitAnswerResponse
{
    public QuestionResponse? NextQuestion { get; set; }
    public int Progress { get; set; }
    public int TotalQuestions { get; set; }
}
