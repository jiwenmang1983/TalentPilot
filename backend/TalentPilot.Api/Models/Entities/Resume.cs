using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TalentPilot.Api.Models.Entities;

[Table("Resumes")]
public class Resume
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [MaxLength(100)]
    [Column("CandidateName")]
    public string? CandidateName { get; set; }

    [MaxLength(30)]
    [Column("Phone")]
    public string? Phone { get; set; }

    [MaxLength(100)]
    [Column("Email")]
    public string? Email { get; set; }

    [MaxLength(500)]
    [Column("RawFilePath")]
    public string? RawFilePath { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("Source")]
    public string Source { get; set; } = "Manual";

    [Column("SourceJobPostId")]
    public int? SourceJobPostId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("ParsedStatus")]
    public string ParsedStatus { get; set; } = "Pending";

    [Column("CandidateId")]
    public int? CandidateId { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }
}

public enum ParsedStatus
{
    Pending,
    Parsing,
    Success,
    Failed
}
