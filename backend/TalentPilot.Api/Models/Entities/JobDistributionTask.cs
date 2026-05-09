using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TalentPilot.Api.Models.Entities;

[Table("JobDistributionTasks")]
public class JobDistributionTask
{
    [Key]
    public long Id { get; set; }

    public int JobPostId { get; set; }

    [Required]
    [MaxLength(20)]
    public string ChannelType { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string TaskStatus { get; set; } = "pending";

    public DateTime? ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    [MaxLength(500)]
    public string? FailureReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(JobPostId))]
    public JobPost? JobPost { get; set; }

    public ICollection<JobDistributionLog> Logs { get; set; } = new List<JobDistributionLog>();
}
