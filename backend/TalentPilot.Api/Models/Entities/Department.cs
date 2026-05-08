using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TalentPilot.Api.Models.Entities;

[Table("Departments")]
public class Department
{
    [Key]
    [Column("Id")]
    public long Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("DepartmentName")]
    public string DepartmentName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("DepartmentKey")]
    public string DepartmentKey { get; set; } = string.Empty;

    [Column("ParentId")]
    public long? ParentId { get; set; }

    [Column("Level")]
    public int Level { get; set; }

    [Column("SortOrder")]
    public int SortOrder { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [Column("UpdatedAt")]
    public DateTime UpdatedAt { get; set; }

    [Column("IsDeleted")]
    public bool IsDeleted { get; set; }

    [ForeignKey("ParentId")]
    public virtual Department? Parent { get; set; }

    public virtual ICollection<Department> Children { get; set; } = new List<Department>();
}