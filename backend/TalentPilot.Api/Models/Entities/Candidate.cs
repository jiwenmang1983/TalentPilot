using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TalentPilot.Api.Models.Entities;

[Table("Candidates")]
public class Candidate
{
    [Key]
    [Column("Id")]
    public long Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("Name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("Email")]
    public string? Email { get; set; }

    [MaxLength(20)]
    [Column("Phone")]
    public string? Phone { get; set; }

    [MaxLength(50)]
    [Column("Gender")]
    public string? Gender { get; set; }

    [Column("Age")]
    public int? Age { get; set; }

    [MaxLength(200)]
    [Column("Education")]
    public string? Education { get; set; }

    [MaxLength(200)]
    [Column("CurrentPosition")]
    public string? CurrentPosition { get; set; }

    [MaxLength(200)]
    [Column("CurrentCompany")]
    public string? CurrentCompany { get; set; }

    [Column("WorkExperience")]
    public int? WorkExperience { get; set; }

    [Column("ExpectedSalary")]
    public decimal? ExpectedSalary { get; set; }

    [MaxLength(50)]
    [Column("ResumeUrl")]
    public string? ResumeUrl { get; set; }

    [MaxLength(50)]
    [Column("Source")]
    public string? Source { get; set; }

    [MaxLength(2000)]
    [Column("Remark")]
    public string? Remark { get; set; }

    [Column("IsDeleted")]
    public bool IsDeleted { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [Column("UpdatedAt")]
    public DateTime UpdatedAt { get; set; }
}

[Table("CandidateConsents")]
public class CandidateConsent
{
    [Key]
    [Column("Id")]
    public long Id { get; set; }

    [Column("CandidateId")]
    public long CandidateId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("ConsentType")]
    public string ConsentType { get; set; } = string.Empty;

    [MaxLength(45)]
    [Column("IpAddress")]
    public string? IpAddress { get; set; }

    [Column("ConsentDate")]
    public DateTime ConsentDate { get; set; }

    [Column("RevokedDate")]
    public DateTime? RevokedDate { get; set; }

    [Column("IsActive")]
    public bool IsActive { get; set; }

    [ForeignKey("CandidateId")]
    public virtual Candidate? Candidate { get; set; }
}