using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TalentPilot.Api.Models.Entities;

[Table("JobDistributionLogs")]
public class JobDistributionLog
{
    [Key]
    public long Id { get; set; }

    public long TaskId { get; set; }

    [Required]
    [MaxLength(10)]
    public string LogLevel { get; set; } = "info";

    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(TaskId))]
    public JobDistributionTask? Task { get; set; }
}
