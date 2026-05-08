using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TalentPilot.Api.Models.Entities;

[Table("Roles")]
public class Role
{
    [Key]
    [Column("Id")]
    public long Id { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("RoleName")]
    public string RoleName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("RoleKey")]
    public string RoleKey { get; set; } = string.Empty;

    [MaxLength(200)]
    [Column("Description")]
    public string? Description { get; set; }

    [Column("IsSystem")]
    public bool IsSystem { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [Column("UpdatedAt")]
    public DateTime UpdatedAt { get; set; }

    [Column("IsDeleted")]
    public bool IsDeleted { get; set; }
}