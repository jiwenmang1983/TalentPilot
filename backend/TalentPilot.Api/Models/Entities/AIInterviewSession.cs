using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TalentPilot.Api.Models.Entities;

[Table("AIInterviewSessions")]
public class AIInterviewSession
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("InterviewInvitationId")]
    public int InterviewInvitationId { get; set; }

    [Column("CandidateId")]
    public long CandidateId { get; set; }

    [Column("JobPostId")]
    public int JobPostId { get; set; }

    [Required]
    [MaxLength(500)]
    [Column("SessionToken")]
    public string SessionToken { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("Status")]
    public string Status { get; set; } = AIInterviewSessionStatus.Pending.ToString();

    [Column("StartTime")]
    public DateTime? StartTime { get; set; }

    [Column("EndTime")]
    public DateTime? EndTime { get; set; }

    [Column("DurationSeconds")]
    public int DurationSeconds { get; set; }

    [MaxLength(1000)]
    [Column("RecordingUrl")]
    public string? RecordingUrl { get; set; }

    [Column("Transcript")]
    public string? Transcript { get; set; }

    [MaxLength(20)]
    [Column("OverallScore")]
    public string? OverallScore { get; set; }

    [Column("AiComments")]
    public string? AiComments { get; set; }

    [Column("GeneratedQuestions")]
    public string? GeneratedQuestions { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("UpdatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(InterviewInvitationId))]
    public virtual InterviewInvitation? InterviewInvitation { get; set; }

    [ForeignKey(nameof(CandidateId))]
    public virtual Candidate? Candidate { get; set; }

    [ForeignKey(nameof(JobPostId))]
    public virtual JobPost? JobPost { get; set; }
}

public static class AIInterviewSessionStatus
{
    public const string Pending = "Pending";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
    public const string NoShow = "NoShow";
}
