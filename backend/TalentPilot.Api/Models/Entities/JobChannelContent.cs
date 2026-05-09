using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TalentPilot.Api.Models.Entities;

[Table("JobChannelContents")]
public class JobChannelContent
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Required]
    [Column("JobPostId")]
    public int JobPostId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("ChannelType")]
    public string ChannelType { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("ChannelName")]
    public string ChannelName { get; set; } = string.Empty;

    [MaxLength(200)]
    [Column("AdaptedTitle")]
    public string? AdaptedTitle { get; set; }

    [Column("AdaptedContent")]
    public string? AdaptedContent { get; set; }

    [Column("WordCount")]
    public int WordCount { get; set; }

    [Column("SkillTags", TypeName = "json")]
    public string? SkillTags { get; set; }

    [Column("SalaryMin")]
    public int? SalaryMin { get; set; }

    [Column("SalaryMax")]
    public int? SalaryMax { get; set; }

    [MaxLength(20)]
    [Column("Status")]
    public string Status { get; set; } = "pending";

    [Column("AdaptationPrompt")]
    public string? AdaptationPrompt { get; set; }

    [MaxLength(500)]
    [Column("ErrorMessage")]
    public string? ErrorMessage { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [Column("UpdatedAt")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("JobPostId")]
    public virtual JobPost? JobPost { get; set; }
}