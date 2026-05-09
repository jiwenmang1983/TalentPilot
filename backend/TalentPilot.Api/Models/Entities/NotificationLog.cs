using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TalentPilot.Api.Models.Entities;

[Table("NotificationLogs")]
public class NotificationLog
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("CandidateId")]
    public long CandidateId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("NotificationType")]
    public string NotificationType { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("Channel")]
    public string Channel { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("Recipient")]
    public string Recipient { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("Subject")]
    public string? Subject { get; set; }

    [Column("Content")]
    public string? Content { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("Status")]
    public string Status { get; set; } = "Pending";

    [Column("SentAt")]
    public DateTime? SentAt { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(1000)]
    [Column("ErrorMessage")]
    public string? ErrorMessage { get; set; }

    [ForeignKey(nameof(CandidateId))]
    public virtual Candidate? Candidate { get; set; }
}

public enum NotificationType
{
    InterviewInvitation,
    InterviewReminder,
    Offer
}

public enum NotificationChannel
{
    Email,
    SMS
}

public enum NotificationStatus
{
    Pending,
    Sent,
    Failed
}