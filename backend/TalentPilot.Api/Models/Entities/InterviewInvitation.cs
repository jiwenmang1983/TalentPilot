using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TalentPilot.Api.Models.Entities;

[Table("InterviewInvitations")]
public class InterviewInvitation
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("CandidateId")]
    public int CandidateId { get; set; }

    [Column("JobPostId")]
    public int JobPostId { get; set; }

    [Column("InvitedByUserId")]
    public int InvitedByUserId { get; set; }

    [Column("InterviewTime")]
    public DateTime? InterviewTime { get; set; }

    [Required]
    [Column("TimeSlotStart")]
    public DateTime TimeSlotStart { get; set; }

    [Required]
    [Column("TimeSlotEnd")]
    public DateTime TimeSlotEnd { get; set; }

    [Required]
    [MaxLength(30)]
    [Column("Status")]
    public string Status { get; set; } = "PendingConfirmation";

    [Required]
    [MaxLength(100)]
    [Column("InviteToken")]
    public string InviteToken { get; set; } = Guid.NewGuid().ToString();

    [Column("InviteSentAt")]
    public DateTime? InviteSentAt { get; set; }

    [Column("ConfirmedAt")]
    public DateTime? ConfirmedAt { get; set; }

    [Column("RefusedAt")]
    public DateTime? RefusedAt { get; set; }

    [Column("Notes")]
    public string? Notes { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CandidateId))]
    public virtual Candidate? Candidate { get; set; }

    [ForeignKey(nameof(JobPostId))]
    public virtual JobPost? JobPost { get; set; }

    [ForeignKey(nameof(InvitedByUserId))]
    public virtual User? InvitedByUser { get; set; }
}

public enum InterviewInvitationStatus
{
    PendingConfirmation,
    Confirmed,
    Refused,
    Cancelled
}
