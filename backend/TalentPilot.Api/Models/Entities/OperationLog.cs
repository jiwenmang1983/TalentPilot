using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TalentPilot.Api.Models.Entities;

[Table("OperationLogs")]
public class OperationLog
{
    [Key]
    [Column("Id")]
    public long Id { get; set; }

    [Column("UserId")]
    public long UserId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("Action")]
    public string Action { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("EntityType")]
    public string EntityType { get; set; } = string.Empty;

    [Column("EntityId")]
    public long EntityId { get; set; }

    [MaxLength(2000)]
    [Column("Detail")]
    public string? Detail { get; set; }

    [MaxLength(50)]
    [Column("IpAddress")]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    [Column("UserAgent")]
    public string? UserAgent { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}