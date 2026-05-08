using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TalentPilot.Api.Models.Entities;

[Table("UserLoginAttempts")]
public class UserLoginAttempt
{
    [Key]
    [Column("Id")]
    public long Id { get; set; }

    [Column("UserId")]
    public long? UserId { get; set; }

    [MaxLength(50)]
    [Column("IpAddress")]
    public string? IpAddress { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("AttemptResult")]
    public string AttemptResult { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("FailedReason")]
    public string? FailedReason { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}