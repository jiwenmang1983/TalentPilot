using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TalentPilot.Api.Models.Entities;

[Table("Users")]
public class User
{
    [Key]
    [Column("Id")]
    public long Id { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("Username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("PasswordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("FullName")]
    public string? FullName { get; set; }

    [MaxLength(20)]
    [Column("Phone")]
    public string? Phone { get; set; }

    [MaxLength(500)]
    [Column("AvatarUrl")]
    public string? AvatarUrl { get; set; }

    [Column("DepartmentId")]
    public long DepartmentId { get; set; }

    [Column("RoleId")]
    public long RoleId { get; set; }

    [Column("IsActive")]
    public bool IsActive { get; set; }

    [Column("LastLoginAt")]
    public DateTime? LastLoginAt { get; set; }

    [MaxLength(50)]
    [Column("LastLoginIp")]
    public string? LastLoginIp { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [Column("UpdatedAt")]
    public DateTime UpdatedAt { get; set; }

    [Column("IsDeleted")]
    public bool IsDeleted { get; set; }

    [MaxLength(500)]
    [Column("RefreshToken")]
    public string? RefreshToken { get; set; }

    [Column("RefreshTokenExpiryTime")]
    public DateTime? RefreshTokenExpiryTime { get; set; }

    [ForeignKey("DepartmentId")]
    public virtual Department? Department { get; set; }

    [ForeignKey("RoleId")]
    public virtual Role? Role { get; set; }

    public virtual ICollection<UserLoginAttempt> LoginAttempts { get; set; } = new List<UserLoginAttempt>();
}