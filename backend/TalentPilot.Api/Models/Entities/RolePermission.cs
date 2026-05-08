using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TalentPilot.Api.Models.Entities;

[Table("RolePermissions")]
public class RolePermission
{
    [Key]
    [Column("Id")]
    public long Id { get; set; }

    [Column("RoleId")]
    public long RoleId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("PermissionKey")]
    public string PermissionKey { get; set; } = string.Empty;

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("RoleId")]
    public virtual Role? Role { get; set; }
}