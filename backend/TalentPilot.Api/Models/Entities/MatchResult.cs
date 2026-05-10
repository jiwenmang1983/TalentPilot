using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TalentPilot.Api.Models.Entities;

[Table("MatchResults")]
public class MatchResult
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("ResumeId")]
    public int ResumeId { get; set; }

    [Column("JobPostId")]
    public int JobPostId { get; set; }

    [Column("Score")]
    public decimal Score { get; set; }

    [Column("MatchedSkills")]
    public string? MatchedSkills { get; set; }

    [Column("MissingSkills")]
    public string? MissingSkills { get; set; }

    [Column("Summary")]
    public string? Summary { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("Status")]
    public string Status { get; set; } = "Pending";

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [Column("DimensionScores")]
    public string? DimensionScores { get; set; }

    [Column("DimensionWeights")]
    public string? DimensionWeights { get; set; }

    [Column("MatchThreshold")]
    public decimal? MatchThreshold { get; set; }
}

public enum MatchStatus
{
    Pending,
    Reviewed,
    Accepted,
    Rejected
}
