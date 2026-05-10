using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TalentPilot.Api.Models.Entities;

[Table("JobPosts")]
public class JobPost
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("Title")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("Department")]
    public string? Department { get; set; }

    [Column("Description")]
    public string? Description { get; set; }

    [Column("Requirements")]
    public string? Requirements { get; set; }

    [Column("SalaryMin")]
    public decimal? SalaryMin { get; set; }

    [Column("SalaryMax")]
    public decimal? SalaryMax { get; set; }

    [MaxLength(50)]
    [Column("Experience")]
    public string? Experience { get; set; }

    [MaxLength(50)]
    [Column("Education")]
    public string? Education { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("Status")]
    public string Status { get; set; } = "Draft";

    [MaxLength(100)]
    [Column("CreatedBy")]
    public string? CreatedBy { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [Column("UpdatedAt")]
    public DateTime? UpdatedAt { get; set; }

    [Column("IsDeleted")]
    public bool IsDeleted { get; set; }

    [Column("MatchThreshold")]
    public decimal? MatchThreshold { get; set; }

    [Column("MatchWeights")]
    public string? MatchWeights { get; set; }

    [Column("InterviewQuestions")]
    public string? InterviewQuestions { get; set; }

    [Column("InterviewDuration")]
    public int InterviewDuration { get; set; } = 30;
}

public enum JobPostStatus
{
    Draft,
    Published,
    Paused,
    Closed
}
