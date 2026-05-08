namespace TalentPilot.Api.Models.DTOs;

public class InterviewReportResponse
{
    public int Id { get; set; }
    public long AIInterviewSessionId { get; set; }
    public long CandidateId { get; set; }
    public string? CandidateName { get; set; }
    public int JobPostId { get; set; }
    public string? JobPostTitle { get; set; }
    public decimal OverallScore { get; set; }
    public string ScoreText { get; set; } = "";
    public Dictionary<string, decimal>? DimensionScores { get; set; }
    public string? AiComments { get; set; }
    public string Recommendation { get; set; } = "";
    public List<string>? Highlights { get; set; }
    public List<string>? Concerns { get; set; }
    public string? HrNotes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class InterviewReportListItem
{
    public int Id { get; set; }
    public string? CandidateName { get; set; }
    public string? JobPostTitle { get; set; }
    public decimal OverallScore { get; set; }
    public string ScoreText { get; set; } = "";
    public string Recommendation { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class UpdateHrNotesRequest
{
    public string HrNotes { get; set; } = "";
}
