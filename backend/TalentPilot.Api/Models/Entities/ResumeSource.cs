using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TalentPilot.Api.Models.Entities;

[Table("ResumeSources")]
public class ResumeSource
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("Channel")]
    public string Channel { get; set; } = string.Empty;

    [Column("Config")]
    public string? Config { get; set; }

    [Column("IsActive")]
    public bool IsActive { get; set; } = true;

    [Column("LastSyncAt")]
    public DateTime? LastSyncAt { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }
}

public enum ResumeChannel
{
    Zhaopin,
    Boss,
    Lagou,
    Liepin,
    ZhiLian,
    QianCheng,
    Manual
}
