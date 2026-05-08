using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TalentPilot.Api.Models.Entities;

[Table("InterviewReports")]
public class InterviewReport
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("AIInterviewSessionId")]
    public long AIInterviewSessionId { get; set; }

    [Column("CandidateId")]
    public long CandidateId { get; set; }

    [Column("JobPostId")]
    public int JobPostId { get; set; }

    [Column("OverallScore")]
    public decimal OverallScore { get; set; }

    [Column("ScoreText")]
    [MaxLength(20)]
    public string ScoreText { get; set; } = "";

    [Column("DimensionScores")]
    public string? DimensionScores { get; set; }

    [Column("AiComments")]
    public string? AiComments { get; set; }

    [Column("Recommendation")]
    [MaxLength(50)]
    public string Recommendation { get; set; } = "";

    [Column("Highlights")]
    public string? Highlights { get; set; }

    [Column("Concerns")]
    public string? Concerns { get; set; }

    [Column("HrNotes")]
    public string? HrNotes { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(AIInterviewSessionId))]
    public virtual AIInterviewSession? AIInterviewSession { get; set; }

    [ForeignKey(nameof(CandidateId))]
    public virtual Candidate? Candidate { get; set; }

    [ForeignKey(nameof(JobPostId))]
    public virtual JobPost? JobPost { get; set; }
}
