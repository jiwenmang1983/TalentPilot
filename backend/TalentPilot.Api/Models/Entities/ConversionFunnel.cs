using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TalentPilot.Api.Models.Entities;

[Table("ConversionFunnels")]
public class ConversionFunnel
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("JobPostId")]
    public int JobPostId { get; set; }

    [Column("PeriodStart")]
    public DateTime PeriodStart { get; set; }

    [Column("PeriodEnd")]
    public DateTime PeriodEnd { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("Stage")]
    public string Stage { get; set; } = string.Empty;

    [Column("Count")]
    public int Count { get; set; }

    [Column("ConversionRate")]
    public decimal ConversionRate { get; set; }

    [Column("AvgTimeSpent")]
    public decimal AvgTimeSpent { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(JobPostId))]
    public virtual JobPost? JobPost { get; set; }
}